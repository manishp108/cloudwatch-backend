using Azure;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using BackEnd.Entities;
using BackEnd.Models;
using Azure.Storage.Blobs.Models;


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

