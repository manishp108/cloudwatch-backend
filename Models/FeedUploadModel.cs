using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace BackEnd.Models
{
    public class FeedUploadModel
    {
        [JsonPropertyName("file")]
        public IFormFile File { get; set; } = null!; // Use IFormFile to handle file upload

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = string.Empty; // User-selected file name

    }
}
