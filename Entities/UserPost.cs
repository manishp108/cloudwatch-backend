using Newtonsoft.Json;

namespace BackEnd.Entities
{
    public class UserPost
    {
        [JsonProperty(PropertyName = "id")]
        public string Id
        {
            get
            {
                return PostId;
            }
        }

        [JsonProperty(PropertyName = "postId")]
        public string PostId { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type
        {
            get
            {
                return "post";
            }
        }


        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "content")]
        public string Content { get; set; }

        [JsonProperty(PropertyName = "caption")]  
        public string Caption { get; set; }

        [JsonProperty(PropertyName = "userId")]
        public string AuthorId { get; set; }

        [JsonProperty(PropertyName = "userUsername")]
        public string AuthorUsername { get; set; }

        [JsonProperty(PropertyName = "dateCreated")]
        public DateTime DateCreated { get; set; }

    }
}
