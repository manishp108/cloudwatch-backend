using Azure.Storage.Blobs;
using BackEnd.Entities;
using BackEnd.Models;
using Microsoft.AspNetCore.Mvc;
using BackEnd.Shared;


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
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating profile picture: {ex.Message}");
            }

        }

    }
}