using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackEnd.Entities
{
    public class BlogUser{      // Entity representing a user stored in Cosmos DB

        // Cosmos DB document id (mapped from UserId)
        [JsonProperty(PropertyName = "id")]
        public string Id
        {
            get
            {
                return UserId;
            }
        }

        // Unique identifier of the user
        [JsonProperty(PropertyName = "userId")]
        public string UserId { get; set; }

        [JsonProperty(PropertyName = "isVerified")]
        public bool IsVerified { get; set; }          // Indicates whether the user account is verified

        [JsonProperty(PropertyName = "username")]
        public string Username { get; set; }   // Maps the Username property to 'username' field in Cosmos DB

        [JsonProperty(PropertyName = "action")]
        public string Action { get; set; }

        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }

        [JsonProperty(PropertyName = "profilePicUrl")]
        public string ProfilePicUrl { get; set; }

        [JsonProperty(PropertyName = "firstname")]
        public string? FirstName { get; set; }

        [JsonProperty(PropertyName = "lastname")]
        public string? LastName { get; set; }
    }
}
