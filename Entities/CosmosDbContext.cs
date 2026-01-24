using Microsoft.Azure.Cosmos;

namespace BackEnd.Entities
{
    public class CosmosDbContext
    {

        public Container PostsContainer { get; }  // Expose Cosmos DB container for posts collection


        public CosmosDbContext(CosmosClient cosmosClient, IConfiguration configuration)
        {
            var databaseName = configuration["CosmosDbSettings:DatabaseName"];      // Read Cosmos DB database name from configuration

            var postsContainerName = configuration["CosmosDbSettings:PostsContainerName"];




            PostsContainer = cosmosClient.GetContainer(databaseName, postsContainerName);      // Initialize Cosmos DB container for posts


        }
    }
    }
