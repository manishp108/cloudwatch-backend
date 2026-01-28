using Azure.Storage.Blobs;
using BackEnd.Entities;
using BackEnd.Models;
using BackEnd.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System.Net;


namespace BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {

        private readonly CosmosDbContext _dbContext;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _profileContainer = "profilepics";  // Blob container for storing profile pictures

        public UsersController(CosmosDbContext dbContext, BlobServiceClient blobServiceClient)
        {
            // Constructor reserved for future dependency injection
            // (e.g., user services, database context, logger)

            _dbContext = dbContext;
            _blobServiceClient = blobServiceClient;
        }

        [HttpPost("updateUser")]
        public async Task<IActionResult> UpdateUser([FromForm] FeedUploadModel model)  // Async method used to handle non-blocking I/O operations (DB and file uploads)

        {
            try
            {
                string updatedPicURL = string.Empty;

                if (model.File != null && !string.IsNullOrEmpty(model.UserId) && !string.IsNullOrEmpty(model.FileName) && !string.IsNullOrEmpty(model.ProfilePic))
                {
                    var containerClient = _blobServiceClient.GetBlobContainerClient(_profileContainer);
                    var blobName = Utility.GetFileNameFromUrl(model.ProfilePic);
                    var blobClient = containerClient.GetBlobClient(blobName);

                    using (var stream = model.File.OpenReadStream())
                    {
                        await blobClient.UploadAsync(stream, overwrite: true);
                    }

                    updatedPicURL = blobClient.Uri.ToString(); // Direct Blob Storage URL
                }

                var itemToUpdate = new BlogUser
                {
                    UserId = model.UserId,        // User identifier
                    Username = model.UserName,    // Updated username
                    ProfilePicUrl = updatedPicURL   // Updated profile picture URL
                };  

                var updatedItem = await _dbContext.UsersContainer.UpsertItemAsync(itemToUpdate);
                Console.WriteLine("Item updated successfully: " + updatedItem);
                return Ok(new { Message = "Profile updated successfully.", UserId = model.UserId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating profile picture: {ex.Message}");
            }

        }


        [HttpGet("getUser")]    // GET Web API endpoint or REST API method or HTTP GET method
        public async Task<IActionResult> GetUser(string userId)
        {

            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("Invalid User Id.");
            }
            try
            {
                // TODO: Fetch user details from Cosmos DB using userId
                var query = _dbContext.UsersContainer.GetItemLinqQueryable<BlogUser>()
                        .Where(x => x.UserId == userId)
                        .ToFeedIterator();
                var user = (await query.ReadNextAsync()).FirstOrDefault();

                if (user != null)
                {
                    return Ok(new
                    {
                        userId = user.Id,
                        email = user.Email,
                        username = user.Username,
                        firstname = user.FirstName,
                        lastname = user.LastName,
                        profilePic = user.ProfilePicUrl,
                        isVerified = user.IsVerified
                    });
                }

                return BadRequest("User not found.");
            }
            catch (CosmosException ex)
            {
                return StatusCode(500, $"Cosmos DB Error: {ex.Message}");
            }
            
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }

           }


        [HttpGet("create")]    
        public async Task<IActionResult> CreateUser()
        {

            try
            {
                // TODO: Implement user creation logic

                var user = new BlogUser
                {
                    UserId = ShortGuidGenerator.Generate(),
                    Username = GenerateRandomName(),
                    ProfilePicUrl = GetRandomProfilePic() // Updated for Blob Storage URLs
                };

                try
                {
                    user.Action = "Create";
                    await _dbContext.UsersContainer.CreateItemAsync(user, new PartitionKey(user.UserId));

                    return Ok(new
                    {
                        userId = user.UserId,
                        username = user.Username,
                        profilePic = user.ProfilePicUrl
                    });
                }
                catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
                {
                    throw ex;
                }
            }
            catch (CosmosException ex)
            {
                return StatusCode(500, $"Cosmos DB Error: {ex.Message}");
            }
               
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        }
}