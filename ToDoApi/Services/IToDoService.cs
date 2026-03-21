using ToDoApi.Models;

namespace ToDoApi.Services
{
    public interface IToDoService
    {
        Task<IEnumerable<ToDoResponse>> GetAllAsync();
        Task<ToDoResponse> CreateAsync(CreateToDoRequest request);
        Task<bool> UpdateAsync(Guid id, UpdateToDoRequest request);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ChangeStatusAsync(Guid id, bool isCompleted);
    }
}
