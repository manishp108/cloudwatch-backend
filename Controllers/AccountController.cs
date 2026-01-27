
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
            
            try
            {
                var uniqueUsername = new UniqueUsername { Username = user.Username };

                //First create a user with a partitionkey as "unique_username" and the new username.  Using the same partitionKey "unique_username" will put all of the username in the same logical partition.
                //  Since there is a Unique Key on /username (per logical partition), trying to insert a duplicate username with partition key "unique_username" will cause a Conflict.
                //  This question/answer https://stackoverflow.com/a/62438454/21579

                await _dbContext.UsersContainer.CreateItemAsync<UniqueUsername>(uniqueUsername, new PartitionKey(uniqueUsername.UserId));
                user.Action = "Create";
                //if we get past adding a new username for partition key "unique_username", then go ahead and insert the new user.
                await _dbContext.UsersContainer.CreateItemAsync<BlogUser>(user, new PartitionKey(user.UserId));

                m.Message = $"User has been created.";
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                //item already existed.  Optimize for the success path.
                throw ex;// ("", $"User with the username {username} already exists.");
                
            }
           
            return Ok(m);
        }




}
}
