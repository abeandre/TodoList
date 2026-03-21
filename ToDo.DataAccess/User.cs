using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ToDo.DataAccess
{
    public class User : AuditableEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string Salt { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string HashedPassword { get; set; } = string.Empty;

        public ICollection<ToDo> ToDos { get; set; } = new List<ToDo>();
    }
}