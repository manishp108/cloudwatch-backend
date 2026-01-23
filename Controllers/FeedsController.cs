using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using BackEnd.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using System.Management;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedsController : ControllerBase
    {
        private readonly string _feedContainer = "media";
        private readonly BlobServiceClient _blobServiceClient;


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


                return Ok();  
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
    }
}
