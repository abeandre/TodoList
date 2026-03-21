using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ToDo.DataAccess
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

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
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }

        public ICollection<ToDo> ToDos { get; set; } = new List<ToDo>();
    }
}