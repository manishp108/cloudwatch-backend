
using Azure.Storage.Blobs;
using BackEnd.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using System.Net;
using BackEnd.ViewModels;

namespace BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]        // Marks this controller as a Web API controller
    public class AccountController : ControllerBase
    {
        private readonly CosmosDbContext _dbContext;          // Cosmos DB context for database operations
        private readonly BlobServiceClient _blobServiceClient;

        public AccountController(CosmosDbContext dbContext, BlobServiceClient blobServiceClient)          // Constructor injection for required services
        {
            _dbContext = dbContext;   // Assign Cosmos DB context
            _blobServiceClient = blobServiceClient;  // Assign Blob storage client
        }

        
        [Route("Register")]
        [HttpPost]
        public async Task <IActionResult> Register(AccountRegisterViewModel m)
        {
            var username = m.Username.Trim().ToLower();

            var user = new BlogUser
            {
                UserId = Guid.NewGuid().ToString(),   // Generate unique user ID
                Username = username    // Assign normalized username
            };  

           

            return Ok();
        }




}
}
