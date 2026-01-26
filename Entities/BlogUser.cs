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

    }
}
