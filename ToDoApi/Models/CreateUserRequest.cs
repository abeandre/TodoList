using System.ComponentModel.DataAnnotations;

namespace ToDoApi.Models
{
    public class CreateUserRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [MaxLength(50)]
        public string Password { get; set; } = string.Empty;
    }
}
