using AutoMapper;
using Microsoft.Extensions.Logging;
using ToDo.DataAccess.Repositories;
using ToDoApi.Models;

namespace ToDoApi.Services
{
    public class ToDoService(IToDoRepository repository, IMapper mapper, ILogger<ToDoService> logger) : IToDoService
    {
        public async Task<IEnumerable<ToDoResponse>> GetAllAsync()
        {
            logger.LogDebug("Fetching all todos");
            var todos = await repository.GetAllAsync();
            return mapper.Map<IEnumerable<ToDoResponse>>(todos);
        }

        public async Task<ToDoResponse?> GetByIdAsync(Guid id)
        {
            logger.LogDebug("Fetching todo {Id}", id);
            var todo = await repository.GetByIdAsync(id);
            if (todo is null)
                logger.LogWarning("Todo {Id} not found", id);
            return todo is null ? null : mapper.Map<ToDoResponse>(todo);
        }

        public async Task<ToDoResponse> CreateAsync(CreateToDoRequest request)
        {
            var todo = mapper.Map<ToDo.DataAccess.ToDo>(request);
            todo.Id = Guid.NewGuid();
            todo.CreatedAt = DateTime.UtcNow;
            todo.UpdatedAt = DateTime.UtcNow;

            await repository.AddAsync(todo);
            logger.LogInformation("Created todo {Id} with title '{Title}'", todo.Id, todo.Title);
            return mapper.Map<ToDoResponse>(todo);
        }

        public async Task<bool> UpdateAsync(Guid id, UpdateToDoRequest request)
        {
            var todo = await repository.GetByIdAsync(id);
            if (todo is null)
            {
                logger.LogWarning("Update failed — todo {Id} not found", id);
                return false;
            }

            mapper.Map(request, todo);
            todo.UpdatedAt = DateTime.UtcNow;
            await repository.UpdateAsync(todo);
            logger.LogInformation("Updated todo {Id}", id);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var deleted = await repository.DeleteAsync(id);
            if (deleted)
                logger.LogInformation("Deleted todo {Id}", id);
            else
                logger.LogWarning("Delete failed — todo {Id} not found", id);
            return deleted;
        }

        public async Task<bool> ChangeStatusAsync(Guid id, bool isCompleted)
        {
            var changed = await repository.ChangeStatusAsync(id, isCompleted);
            if (changed)
                logger.LogInformation("Todo {Id} marked as {Status}", id, isCompleted ? "completed" : "active");
            else
                logger.LogWarning("Status change failed — todo {Id} not found", id);
            return changed;
        }
    }
}
