using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BackEnd.Entities;
using BackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using System.Drawing.Printing;
using Microsoft.Azure.Cosmos.Linq;


namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedsController : ControllerBase
    {
        private readonly string _feedContainer = "media";
        private readonly BlobServiceClient _blobServiceClient;
        private readonly CosmosDbContext _dbContext;  //d
        private readonly ILogger<FeedsController> _logger;  // Logger instance used to record application logs for this controller
        private readonly string cdnBaseUrl = "https://socialnotebookscdn-ghcdgcdxc8andjgv.z02.azurefd.net/";   //defines a base URL for your CDN (Azure Front Door / Azure CDN).used to construct public media URLs (images/videos) for feeds


        // Constructor with dependency injection for database context, blob storage, and logging
        public FeedsController(CosmosDbContext dbContext, BlobServiceClient blobServiceClient, ILogger<FeedsController> logger)
        {
            _dbContext = dbContext;                    // Assign Cosmos DB context
            _blobServiceClient = blobServiceClient;    // Assign Azure Blob service client
            _logger = logger;                          // Assign logger for diagnostic and error logging
        }


        [HttpPost("uploadFeed")]
        public async Task<IActionResult> UploadFeed([FromForm] FeedUploadModel model, CancellationToken cancellationToken)
        {
            Console.WriteLine("Starting UploadFeed API...");
            try
            {
                if (model.File == null || string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.FileName))
                {
                    Console.WriteLine("Validation failed: Missing required fields.");
                    return BadRequest("Missing required fields.");
                }
                Console.WriteLine($"Received file: {model.File.FileName}, Size: {model.File.Length} bytes");
                Console.WriteLine($"User ID: {model.UserId}, User Name: {model.UserName}");

                var containerClient = _blobServiceClient.GetBlobContainerClient(_feedContainer);
                Console.WriteLine($"Connecting to Blob Container: {_feedContainer}");

                var blobName = $"{ShortGuidGenerator.Generate()}_{Path.GetFileName(model.File.FileName)}";
                var blobClient = containerClient.GetBlobClient(blobName);
                Console.WriteLine($"Generated Blob Name: {blobName}");

                var mimeType = GetMimeType(model.File.FileName);

                // --- NEW: Use UploadAsync() instead of OpenWriteAsync() to avoid 412 errors ---
                using var fileStream = model.File.OpenReadStream();
                var blobHttpHeaders = new BlobHttpHeaders
                {
                    ContentType = mimeType,
                    CacheControl = "public, max-age=31536000" // 1-year cache   
                };

                // Upload file in one step, setting headers immediately
                Console.WriteLine("Starting file upload...");
                await blobClient.UploadAsync(fileStream, blobHttpHeaders, cancellationToken: cancellationToken);
                Console.WriteLine("File uploaded successfully.");

                var blobUrl = blobClient.Uri.ToString();
                Console.WriteLine($"Blob URL: {blobUrl}");

                // Save Post Info in Cosmos DB
                var userPost = new UserPost
                {
                    PostId = ShortGuidGenerator.Generate(),
                    Title = model.ProfilePic,
                    Content = blobUrl,
                    Caption = string.IsNullOrEmpty(model.Caption) || model.Caption == "undefined" ? string.Empty : model.Caption,
                    AuthorId = model.UserId,
                    AuthorUsername = model.UserName,
                    DateCreated = DateTime.UtcNow
                };

                await _dbContext.PostsContainer.UpsertItemAsync(userPost, new PartitionKey(userPost.PostId));
                Console.WriteLine("User post successfully saved to Cosmos DB.");

                return Ok(new
                {
                    Message = "Feed uploaded successfully.",
                    FeedId = userPost.PostId
                });
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Upload was canceled by the client or due to timeout.");
                return StatusCode(499, "Upload was canceled due to timeout or client cancellation.");
            }
            catch (RequestFailedException ex) when (ex.Status == 412) // Handle precondition failure
            {
                Console.WriteLine($"Blob precondition failed: {ex.Message}");
                return StatusCode(412, "Blob precondition failed. Please retry.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during upload: {ex.Message}");
                return StatusCode(500, $"Error uploading feed: {ex.Message}");
            }
        }



        [HttpGet("getUserFeeds")]                // Added GET APIs for user feeds
        public async Task<IActionResult> GetUserFeeds(string? userId = null, int pageNumber = 1, int pageSize = 2)   // used async, recommended for future DB work.
        {
            try
            {                        // TODO: Add DB logic here

                var userPosts = new List<UserPost>();
                // Cosmos DB SQL query to fetch posts with pagination and latest-first ordering
                var queryString = $"SELECT * FROM f WHERE f.type='post' ORDER BY f.dateCreated DESC OFFSET {(pageNumber - 1) * pageSize} LIMIT {pageSize}";
                Console.WriteLine($"Executing query: {queryString}");
                var queryFromPostsContainer = _dbContext.PostsContainer.GetItemQueryIterator<UserPost>(new QueryDefinition(queryString));
                while (queryFromPostsContainer.HasMoreResults)
                {
                    var response = await queryFromPostsContainer.ReadNextAsync();
                    Console.WriteLine($"Fetched {response.Count} posts from Cosmos DB.");
                    userPosts.AddRange(response.ToList());         // Add fetched posts to the final result list

                }
                if (!string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine($"UserId provided: {userId}. Checking user likes...");
                    var likes = await GetLikesAsync();
                    var userLikes = new List<UserPost>();
                    foreach (var item in userPosts)
                    {
                        var hasUserLikedPost = likes.FirstOrDefault(x => x.PostId == item.PostId && x.LikeAuthorId == userId);
                        item.LikeFlag = hasUserLikedPost != null ? 1 : 0;
                    }


                    var hasAnyReportedPost = userPosts.Any(x => x.ReportCount > 0);
                    if (hasAnyReportedPost)
                    {
                        var userReportedPostIds = new List<string>();        // List to store post IDs reported by the current user
                        var query = _dbContext.ReportedPostsContainer.GetItemLinqQueryable<ReportedPost>()
                                    .Where(p => p.ReportedUserId == userId)
                                    .Select(p => p.PostId)
                                    .ToFeedIterator();
                        while (query.HasMoreResults)
                        {
                            var response = await query.ReadNextAsync();
                            userReportedPostIds.AddRange(response.ToList());
                        }

                        if (userReportedPostIds.Count > 0)       // Remove reported posts from the user feed
                        {
                            userPosts = userPosts.Where(x => !userReportedPostIds.Contains(x.Id)).ToList();
                        }
                    }
                }
                Console.WriteLine("Reordering posts by LikeCount, CommentCount, and DateCreated...");

                userPosts = userPosts        // Sort posts by most recent first, then by like count, then by comment count
                    .OrderByDescending(x => x.DateCreated)
                    .ThenByDescending(x => x.LikeCount)
                    .ThenByDescending(x => x.CommentCount)
                    .ToList();

                var userIds = userPosts.Select(x => x.AuthorId).Distinct().ToList();  // Extract distinct author IDs from the reordered posts

                var usersQuery = _dbContext.UsersContainer.GetItemLinqQueryable<BlogUser>()
                                                 .Where(x => userIds.Contains(x.Id))
                                                 .ToFeedIterator();
                var users = new List<BlogUser>();
                while (usersQuery.HasMoreResults)
                {
                    var response = await usersQuery.ReadNextAsync();
                    users.AddRange(response.ToList());
                }
                foreach (var post in userPosts)
                {
                    post.Content = post.Content.Replace("https://socialnotebooksstorage.blob.core.windows.net/", cdnBaseUrl); // Convert blob storage URLs in post content to CDN URLs to improve performance and caching
                    var user = users.FirstOrDefault(x => x.Id == post.AuthorId);
                    post.IsVerified = user != null ? user.IsVerified : false;      // Set verification status based on author data
                }
                Console.WriteLine("Reordering complete.");
                Console.WriteLine("Returning final ordered list of posts.");
                return Ok(new { BlogPostsMostRecent = userPosts });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving feeds: {ex.Message}");      // Log error details for debugging
                return StatusCode(500, $"Error retrieving feeds: {ex.Message}");   // Return internal server error response
            }
        }




        private async Task<List<UserPostLike>> GetLikesAsync()     // Fetch all likes from Cosmos DB asynchronously
        {
            var likes = new List<UserPostLike>();
            var query = _dbContext.LikesContainer.GetItemLinqQueryable<UserPostLike>()    // Create LINQ query to retrieve items from Likes container
                        .ToFeedIterator();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                likes.AddRange(response.ToList());
            }
            return likes;
        }





        [HttpGet("download")]            // // Added GET APIs for download
        public IActionResult Download()
        {
            return Ok();
        }



        private string GetMimeType(string fileName)
        {

            var extension = Path.GetExtension(fileName).ToLower();  // Extracts the file extension from the filename and converts it to lowercase for consistent comparison


            var mimeTypes = new Dictionary<string, string>
            {   
                // Image formats      
                { ".jpg", "image/jpeg" },
                { ".jpeg", "image/jpeg" },
                { ".png", "image/png" },
                { ".gif", "image/gif" },
                { ".bmp", "image/bmp" },       // Mapping of allowed image file extensions to their corresponding MIME types
                { ".svg", "image/svg+xml" },
                { ".webp", "image/webp" },

                // Video formats
                { ".mp4", "video/mp4" },
                { ".mov", "video/quicktime" },
                { ".avi", "video/x-msvideo" },
                { ".wmv", "video/x-ms-wmv" },    // Mapping of allowed video file extensions to their corresponding MIME types
                { ".flv", "video/x-flv" },
                { ".mkv", "video/x-matroska" },
                { ".webm", "video/webm" }
            };
            return mimeTypes.TryGetValue(extension, out var mimeType) ? mimeType : "application/octet-stream";
        }
    }
}

