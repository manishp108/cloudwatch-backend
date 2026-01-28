using Azure.Storage.Blobs;
using BackEnd.Entities;
using Microsoft.AspNetCore.Mvc;


namespace BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {

        private readonly CosmosDbContext _dbContext;
        private readonly BlobServiceClient _blobServiceClient;
        public UsersController(CosmosDbContext dbContext, BlobServiceClient blobServiceClient)
        {
            // Constructor reserved for future dependency injection
            // (e.g., user services, database context, logger)

            _dbContext = dbContext;
            _blobServiceClient = blobServiceClient;
        }

        [HttpPost("updateUser")]
        public IActionResult UpdateUser()
        {
            return Ok();
        }



    }
}