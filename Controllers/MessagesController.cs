
using Azure.Messaging.ServiceBus;
using BackEnd.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Configuration;

namespace BackEnd.Controllers
{
    [ApiController]   // Marks this class as a Web API controller
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly CosmosDbContext _dbContext;          // Cosmos DB context for database operations
        private readonly ServiceBusSender _serviceBusSender;  // Azure Service Bus sender for publishing messages
        private readonly ServiceBusClient _serviceBusClient;  // Azure Service Bus client for managing connections
        private readonly IConfiguration _configuration;       // Application configuration settings
        public MessagesController(CosmosDbContext dbContext,
            ServiceBusSender serviceBusSender,
            ServiceBusClient serviceBusClient,
            IConfiguration configuration)
        {
            // Constructor reserved for future dependency injection
            // (e.g., services, database context, logger, etc.)
            _dbContext = dbContext;
            _serviceBusSender = serviceBusSender;      // Inject required services via dependency injection
            _serviceBusClient = serviceBusClient;
            _configuration = configuration;
        }


        [Route("chat-users/{userId}")]   // Route to fetch users the given user has chatted with
        [HttpGet]                        // Handles HTTP GET requests
        public  IActionResult GetUsersChattedWith()
        {

            // Here we will Implement logic to fetch users the given user has chatted with.
            // In short involve querying Cosmos DB using the userId  
            return Ok();
        }


    }
}