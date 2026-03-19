using System;

namespace ToDo.DataAccess
{
    public class ToDo
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
