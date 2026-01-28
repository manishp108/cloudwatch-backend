using Newtonsoft.Json;

namespace BackEnd.Entities
{
    public class Message
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("chatId")]
        public string ChatId { get; set; }

    

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
