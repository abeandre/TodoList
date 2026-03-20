using System.ComponentModel.DataAnnotations;

namespace ToDoApi.Models
{
    public class UpdateToDoRequest
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;
    }
}
