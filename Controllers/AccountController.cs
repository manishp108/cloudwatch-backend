
using Azure.Storage.Blobs;
using BackEnd.Entities;
using BackEnd.ViewModels;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using System.Net;
using Microsoft.Azure.Cosmos.Linq;  // Azure Cosmos DB LINQ support

namespace BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]        // Marks this controller as a Web API controller
    public class AccountController : ControllerBase
    {
        private readonly CosmosDbContext _dbContext;          // Cosmos DB context for database operations
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _profileContainer = "profilepic"; // Blob container name for storing profile pictures

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


        [Route("Login")]
        [HttpPost]    // API endpoint for user login
        public async Task<IActionResult> Login(AccountLoginViewModel m)
        {
            var username = m.Username.Trim().ToLower();

            var queryDefinition = new QueryDefinition("SELECT * FROM u WHERE u.type = 'user' AND u.username = @username").WithParameter("@username", username);
            var query = _dbContext.UsersContainer.GetItemQueryIterator<BlogUser>(queryDefinition);

            List<BlogUser> results = new List<BlogUser>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();

                results.AddRange(response.ToList());
            }
            if (results.Count > 1)      // Safety check: there should never be more than one user per username
            {
                throw new Exception($"More than one user found for username '{username}'");
            }

            var u = results.SingleOrDefault();
            return Ok(u);
        }


        [HttpPost("google-verify-token")]  // API endpoint to verify Google authentication token
        public async Task<IActionResult> VerifyToken([FromBody] GoogleTokenRequest googleTokenRequest)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(googleTokenRequest.Token);          // Validate Google ID token and extract user payload
                var query = _dbContext.UsersContainer.GetItemLinqQueryable<BlogUser>()
                        .Where(p => p.Email == payload.Email)
                        .ToFeedIterator();
                var existingUser = (await query.ReadNextAsync()).FirstOrDefault();

                var firstName = payload.GivenName?.Trim();
                var lastName = payload.FamilyName?.Trim();
                var underscoredFirstName = payload.GivenName?.Trim();          // Extract first and last name from Google payload
                var underscoredLastName = payload.FamilyName?.Trim();
                if (underscoredFirstName != null)
                {
                    underscoredFirstName = underscoredFirstName.Replace(' ', '_');
                }
                if (underscoredLastName != null)
                {
                    underscoredLastName = underscoredLastName.Replace(' ', '_');
                }
                var username = $"{underscoredFirstName?.ToLower()}_{underscoredLastName?.ToLower()}"; // Generate username in lowercase format: firstname_lastname

                if (existingUser != null)
                {
                    existingUser.FirstName = firstName;
                    existingUser.LastName = lastName;
                    existingUser.Username = username;
                    existingUser.IsVerified = true;
                    if (!string.IsNullOrEmpty(payload.Picture))
                    {
                        var containerClient = _blobServiceClient.GetBlobContainerClient(_profileContainer);
                        string blobName = $"{existingUser.UserId}-profile-pic.jpg";
                        var blobClient = containerClient.GetBlobClient(blobName);
                        using HttpClient httpClient = new();
                        using var stream = await httpClient.GetStreamAsync(payload.Picture);
                        await blobClient.UploadAsync(stream, overwrite: true);

                        existingUser.ProfilePicUrl = blobClient.Uri.ToString();
                    }
                    await _dbContext.UsersContainer.UpsertItemAsync<BlogUser>(existingUser, new PartitionKey(existingUser.UserId));
                    return Ok(existingUser);
                }

                // Add new user to Cosmos DB
                try
                {
                    string userId = ShortGuidGenerator.Generate();     // Generate a short unique user ID
                    string profilePicBlobUrl = string.Empty;      // Initialize profile picture URL
                    if (!string.IsNullOrEmpty(payload.Picture))
                    {
                        var containerClient = _blobServiceClient.GetBlobContainerClient(_profileContainer);          // Get Azure Blob container for profile images
                        string blobName = $"{userId}-profile-pic.jpg";
                        var blobClient = containerClient.GetBlobClient(blobName);
                        using HttpClient httpClient = new();
                        using var stream = await httpClient.GetStreamAsync(payload.Picture);
                        await blobClient.UploadAsync(stream, overwrite: true);

                        profilePicBlobUrl = blobClient.Uri.ToString();
                    }

                    var user = new BlogUser      // Create a new BlogUser entity from Google profile data
                    {
                        UserId = userId,      // Unique user identifier
                        Email = payload.Email,
                        Username = username,
                        FirstName = firstName,
                        LastName = lastName,
                        ProfilePicUrl = profilePicBlobUrl,  // Profile picture blob URL
                        Action = "Create",
                        IsVerified = true
                    };

                    await _dbContext.UsersContainer.CreateItemAsync<BlogUser>(user, new PartitionKey(user.UserId));      // Persist user record in Cosmos DB


                    return Ok(user);
                } 
            catch (CosmosException ex)
            {
                return BadRequest(ex.Message);          // Handle Cosmos DB related errors
                }
            }
            catch (InvalidJwtException e)
            {
                return Unauthorized();      // Handle invalid or expired Google JWT token
            }
        }


        // API endpoint placeholder for processing user profile pictures
        // This will be extended later to handle profile image migration,
        // optimization, or reprocessing logic
        [HttpGet("process-profile-pics")]
        public async Task<IActionResult> ProcessProfilePictures()
        {
            var query = _dbContext.UsersContainer.GetItemLinqQueryable<BlogUser>(allowSynchronousQueryExecution: true);      // Query Cosmos DB for users having Google-hosted profile pictures
            var googleUsers = query.Where(u => u.ProfilePicUrl != null && u.ProfilePicUrl.Contains("googleusercontent.com")).ToList();      // Filter users whose profile picture URL is from Google

            foreach (var user in googleUsers)
            {
                string newBlobUrl = await DownloadAndSaveProfilePic(user.ProfilePicUrl, user.Id);
                user.ProfilePicUrl = newBlobUrl;

                // Update user in Cosmos DB
                await _dbContext.UsersContainer.ReplaceItemAsync(user, user.Id);
            }
            return Ok("Profile pictures processed.");
        }

        private async Task<string> DownloadAndSaveProfilePic(string profilePicUrl, string userId)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_profileContainer);
            await containerClient.CreateIfNotExistsAsync();

            string blobName = $"{userId}-profile-pic.jpg";
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            using HttpClient httpClient = new HttpClient();
            using var stream = await httpClient.GetStreamAsync(profilePicUrl);
            await blobClient.UploadAsync(stream, overwrite: true);

            return blobClient.Uri.ToString();
        }

    }
}
