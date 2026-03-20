namespace ToDoApi.Models
{
    public class ToDoResponse
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? FinishedAt { get; set; }

        public static ToDoResponse From(ToDo.DataAccess.ToDo todo) => new()
        {
            Id = todo.Id,
            Title = todo.Title,
            Description = todo.Description,
            CreatedAt = todo.CreatedAt,
            FinishedAt = todo.FinishedAt
        };
    }
}
