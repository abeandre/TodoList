using System;
using System.ComponentModel.DataAnnotations;

namespace ToDo.DataAccess
{
    public abstract class AuditableEntity
    {
        [Key]
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
