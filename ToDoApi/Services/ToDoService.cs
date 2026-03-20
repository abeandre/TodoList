using AutoMapper;
using ToDo.DataAccess.Repositories;
using ToDoApi.Models;

namespace ToDoApi.Services
{
    public class ToDoService(IToDoRepository repository, IMapper mapper) : IToDoService
    {
        public async Task<IEnumerable<ToDoResponse>> GetAllAsync()
        {
            var todos = await repository.GetAllAsync();
            return mapper.Map<IEnumerable<ToDoResponse>>(todos);
        }

        public async Task<ToDoResponse?> GetByIdAsync(Guid id)
        {
            var todo = await repository.GetByIdAsync(id);
            return todo is null ? null : mapper.Map<ToDoResponse>(todo);
        }

        public async Task<ToDoResponse> CreateAsync(CreateToDoRequest request)
        {
            var todo = mapper.Map<ToDo.DataAccess.ToDo>(request);
            todo.Id = Guid.NewGuid();
            todo.CreatedAt = DateTime.UtcNow;

            await repository.AddAsync(todo);
            return mapper.Map<ToDoResponse>(todo);
        }

        public async Task<bool> UpdateAsync(Guid id, UpdateToDoRequest request)
        {
            var todo = await repository.GetByIdAsync(id);
            if (todo is null)
                return false;

            mapper.Map(request, todo);
            await repository.UpdateAsync(todo);
            return true;
        }

        public Task<bool> DeleteAsync(Guid id) =>
            repository.DeleteAsync(id);

        public Task<bool> ChangeStatusAsync(Guid id, bool isCompleted) =>
            repository.ChangeStatusAsync(id, isCompleted);
    }
}
