using System.ComponentModel.DataAnnotations;

namespace ToDoApi.Models
{
    public class UpdateUserRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [MinLength(8)]
        [MaxLength(50)]
        public string? Password { get; set; }
    }
}
