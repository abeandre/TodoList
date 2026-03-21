using System.Text.Json.Serialization;

namespace ToDoApi.Models
{
    public class AuthResponse
    {
        [JsonIgnore]
        public string Token { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
