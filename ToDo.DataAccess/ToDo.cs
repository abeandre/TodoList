using System;
using System.ComponentModel.DataAnnotations;

namespace ToDo.DataAccess
{
    public class ToDo : AuditableEntity
    {
        public DateTime? FinishedAt { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
