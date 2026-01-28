using Azure.Storage.Blobs;
using BackEnd.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;


namespace BackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserPostController : ControllerBase
    {
        private readonly CosmosDbContext _dbContext;
        private readonly BlobServiceClient _blobServiceClient;
        public UserPostController(CosmosDbContext dbContext, BlobServiceClient blobServiceClient)
        {
            _dbContext = dbContext;
            _blobServiceClient = blobServiceClient;
        }


        [Route("post/{postId}")]
        [HttpGet]
        public async Task<IActionResult> PostView(string postId, string userId)
        {
            //When getting the blogpost from the Posts container, the id is postId and the partitionKey is also postId.
            //  This will automatically return only the type="post" for this postId (and not the type=comment or any other types in the same partition postId)
            ItemResponse<UserPost> response = await _dbContext.PostsContainer.ReadItemAsync<UserPost>(postId, new PartitionKey(postId));
            var bp = response.Resource;



            var queryString1 = $"SELECT * FROM p WHERE p.type='comment' AND p.postId = @PostId ORDER BY p.dateCreated DESC";
            var queryDef1 = new QueryDefinition(queryString1);
            queryDef1.WithParameter("@PostId", postId);
            var query1 = _dbContext.PostsContainer.GetItemQueryIterator<UserPostComment>(queryDef1);

            List<UserPostComment> comments = new List<UserPostComment>();
            while (query1.HasMoreResults)
            {
                var resp1 = await query1.ReadNextAsync();
                var ru = resp1.RequestCharge;
                comments.AddRange(resp1.ToList());
            }
            var userLikedPost = false;   // Flag to check if the current user has liked this post

            return Ok();
        }
    }
}