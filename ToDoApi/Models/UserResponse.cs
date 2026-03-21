using System;
using System.Text.Json.Serialization;

namespace ToDoApi.Models
{
    public class UserResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        [JsonIgnore]
        public string Token { get; set; } = string.Empty;
    }
}
