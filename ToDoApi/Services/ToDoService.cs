using AutoMapper;
using Microsoft.Extensions.Logging;
using ToDo.DataAccess.Repositories;
using ToDoApi.Models;

namespace ToDoApi.Services
{
    public class ToDoService(IToDoRepository repository, IMapper mapper, ILogger<ToDoService> logger) : IToDoService
    {
        public async Task<IEnumerable<ToDoResponse>> GetAllAsync(Guid userId)
        {
            logger.LogDebug("Fetching all todos for user {UserId}", userId);
            var todos = await repository.GetAllAsync(userId);
            return mapper.Map<IEnumerable<ToDoResponse>>(todos);
        }

        public async Task<ToDoResponse> CreateAsync(Guid userId, CreateToDoRequest request)
        {
            var todo = mapper.Map<ToDo.DataAccess.ToDo>(request);
            todo.Id = Guid.NewGuid();
            todo.UserId = userId;
            todo.CreatedAt = DateTime.UtcNow;
            todo.UpdatedAt = DateTime.UtcNow;

            await repository.AddAsync(todo);
            logger.LogInformation("Created todo {Id} with title '{Title}' for user {UserId}", todo.Id, todo.Title, userId);
            return mapper.Map<ToDoResponse>(todo);
        }

        public async Task<bool> UpdateAsync(Guid id, Guid userId, UpdateToDoRequest request)
        {
            var todo = await repository.GetByIdAsync(id, userId);
            if (todo is null)
            {
                logger.LogWarning("Update failed — todo {Id} not found or unauthorized for user {UserId}", id, userId);
                return false;
            }

            mapper.Map(request, todo);
            todo.UpdatedAt = DateTime.UtcNow;
            await repository.UpdateAsync(todo);
            logger.LogInformation("Updated todo {Id} for user {UserId}", id, userId);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, Guid userId)
        {
            var deleted = await repository.DeleteAsync(id, userId);
            if (deleted)
                logger.LogInformation("Deleted todo {Id} by user {UserId}", id, userId);
            else
                logger.LogWarning("Delete failed — todo {Id} not found or unauthorized", id);
            return deleted;
        }

        public async Task<bool> ChangeStatusAsync(Guid id, Guid userId, bool isCompleted)
        {
            var changed = await repository.ChangeStatusAsync(id, userId, isCompleted);
            if (changed)
                logger.LogInformation("Todo {Id} marked as {Status} by user {UserId}", id, isCompleted ? "completed" : "active", userId);
            else
                logger.LogWarning("Status change failed — todo {Id} not found or unauthorized", id);
            return changed;
        }
    }
}
