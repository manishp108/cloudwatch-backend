using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackEnd.Entities
{
    public class UserPostLike             // This class will be used for mapping Likes data from Cosmos DB

    {

        [JsonProperty(PropertyName = "id")]
        public string Id
        {
            get
            {
                return LikeId;
            }
        }


        [JsonProperty(PropertyName = "postId")]
        public string PostId { get; set; }

        [JsonProperty(PropertyName = "userId")]
        public string LikeAuthorId { get; set; }





    }
}
