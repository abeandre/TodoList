using ToDoApi.Models;

namespace ToDoApi.Services
{
    public interface IToDoService
    {
        Task<IEnumerable<ToDoResponse>> GetAllAsync(Guid userId);
        Task<ToDoResponse> CreateAsync(Guid userId, CreateToDoRequest request);
        Task<bool> UpdateAsync(Guid id, Guid userId, UpdateToDoRequest request);
        Task<bool> DeleteAsync(Guid id, Guid userId);
        Task<bool> ChangeStatusAsync(Guid id, Guid userId, bool isCompleted);
    }
}
