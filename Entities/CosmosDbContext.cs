using Microsoft.Azure.Cosmos;

namespace BackEnd.Entities
{
    public class CosmosDbContext
    {

        public Container PostsContainer { get; }  // Expose Cosmos DB container for posts collection
        public Container LikesContainer { get; }  // Cosmos DB container reference for storing likes data
        public Container ReportedPostsContainer { get; }



        public CosmosDbContext(CosmosClient cosmosClient, IConfiguration configuration)
        {
            var databaseName = configuration["CosmosDbSettings:DatabaseName"];      // Read Cosmos DB database name from configuration

            var postsContainerName = configuration["CosmosDbSettings:PostsContainerName"];
            var likesContainerName = configuration["CosmosDbSettings:LikesContainerName"];
            var reportedPostsContainerName = configuration["CosmosDbSettings:ReportedPostsContainerName"];




            PostsContainer = cosmosClient.GetContainer(databaseName, postsContainerName);      // Initialize Cosmos DB container for posts, likes, 
            LikesContainer = cosmosClient.GetContainer(databaseName, likesContainerName);
            ReportedPostsContainer = cosmosClient.GetContainer(databaseName, reportedPostsContainerName);


        }
    }
    }
