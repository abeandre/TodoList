using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ToDo.DataAccess.Repositories
{
    public interface IToDoRepository
    {
        Task<ToDo?> GetByIdAsync(Guid id, Guid userId);
        Task<IEnumerable<ToDo>> GetAllAsync(Guid userId);
        Task AddAsync(ToDo todo);
        Task UpdateAsync(ToDo todo);
        Task<bool> DeleteAsync(Guid id, Guid userId);
        Task<bool> ChangeStatusAsync(Guid id, Guid userId, bool isCompleted);
    }
}