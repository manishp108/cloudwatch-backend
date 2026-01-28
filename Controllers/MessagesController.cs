
using Azure.Messaging.ServiceBus;
using BackEnd.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;

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
        public async Task<IActionResult> GetUsersChattedWith(string userId)
        {           // Here we will Implement logic to fetch users the given user has chatted with.
                    // In short involve querying Cosmos DB using the userId  
            var chatQuery = _dbContext.ChatsContainer.GetItemLinqQueryable<Chat>()
                                                 .Where(m => m.SenderId == userId || m.RecipientId == userId)
                                                 .OrderByDescending(m => m.Timestamp)  // Latest chats first
                                                 .ToFeedIterator();

            var chats = new List<Chat>();
            while (chatQuery.HasMoreResults)
            {
                var response = await chatQuery.ReadNextAsync();
                chats.AddRange(response.ToList());
            }

            var userIds = chats
                .Select(m => m.SenderId == userId ? m.RecipientId : m.SenderId)
                .Distinct()
                .ToList();

            var userQuery = _dbContext.UsersContainer.GetItemLinqQueryable<BlogUser>()
                                          .Where(u => userIds.Contains(u.Id))         // Fetch user details for all chatted user IDs
                                          .ToFeedIterator();

            var users = new List<BlogUser>();
            while (userQuery.HasMoreResults)
            {
                var response = await userQuery.ReadNextAsync();
                users.AddRange(response.ToList());
            }

            var chatUsers = new List<ChatUser>();
            foreach (var user in users)
            {
                var chat = chats.First(x => x.SenderId == user.Id || x.RecipientId == user.Id);
                chatUsers.Add(new ChatUser()
                {
                    UserId = user.Id,
                    Email = user.Email,
                    ProfilePicUrl = user.ProfilePicUrl,
                    Username = user.Username,
                    ChatId = chat.Id,
                    TimeStamp = chat.Timestamp
                });
            }
            return Ok(chatUsers.OrderByDescending(x => x.TimeStamp));
        }


        [Route("chat-history/{chatId}")]    // Route to fetch chat history using chatId
        [HttpGet]                     // Handles HTTP GET requests
        public async Task<IActionResult> GetChatHistory(string chatId)
        {
            // We will Implement logic to fetch chat history for the given chatId

            var messageQuery = _dbContext.MessagesContainer.GetItemLinqQueryable<Message>()  // Fetch all messages belonging to the specified chatId
                                                 .Where(m => m.ChatId == chatId)
                                                 .OrderBy(m => m.Timestamp)
                                                 .ToFeedIterator();

            var messages = new List<Message>();
            while (messageQuery.HasMoreResults)
            {
                var response = await messageQuery.ReadNextAsync();
                messages.AddRange(response.ToList());
            }
            return Ok(messages);
        }

        [Route("send-message")]
        [HttpPost]     // Handles HTTP POST requests
        public async Task<IActionResult> SendMessage([FromBody] SendMessage request)
        {

            //Here we will Implement logic to send a message
            if (request != null && request.SenderId != null && request.RecipientId != null && request.Content != null)
            {
                var existingChat = new Chat();
                if (request.ChatId == null)
                {
                    existingChat = _dbContext.ChatsContainer.GetItemLinqQueryable<Chat>()
                        .Where(c => (c.SenderId == request.SenderId && c.RecipientId == request.RecipientId) || (c.RecipientId == request.SenderId && c.SenderId == request.RecipientId))
                        .FirstOrDefault();
                    if (existingChat == null)
                    {
                        request.ChatId = ShortGuidGenerator.Generate();
                        var chat = new Chat
                        {
                            Id = request.ChatId,
                            SenderId = request.SenderId,
                            RecipientId = request.RecipientId,
                            Timestamp = DateTime.UtcNow
                        };
                        await _dbContext.ChatsContainer.CreateItemAsync(chat, new PartitionKey(chat.Id));  // using Microsoft.Azure.Cosmos;
                    }
                    else
                    {
                        request.ChatId = existingChat.Id;
                    }
                    }
                    else
                    {
                        existingChat = _dbContext.ChatsContainer.GetItemLinqQueryable<Chat>()
                            .Where(c => (c.Id == request.ChatId))
                            .FirstOrDefault();
                        if (existingChat == null)      // Return error if ChatId is invalid
                    {
                        return BadRequest("Incorrect ChatId. Chat not found.");
                        }
                        existingChat.Timestamp = DateTime.UtcNow;
                        await _dbContext.ChatsContainer.UpsertItemAsync(existingChat, new PartitionKey(existingChat.Id));     // Upsert chat record to Cosmos DB
                    }

                    var message = new Message  // Create a new message entity
                    {
                        Id = ShortGuidGenerator.Generate(),
                        ChatId = request.ChatId,
                        SenderId = request.SenderId,
                        RecipientId = request.RecipientId,
                        Content = request.Content,
                        Timestamp = DateTime.UtcNow   // Message sent time
                    };

                    await _dbContext.MessagesContainer.CreateItemAsync(message, new PartitionKey(message.Id));

                    // Publish message to Service Bus
                    await PublishMessageToServiceBus(message);   // Publish message to Azure Service Bus for downstream processing / notifications


                return Ok(new { request.ChatId });
                }
                return BadRequest("Invalid Request Data");
            }




    }
}