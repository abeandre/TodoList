using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ToDo.DataAccess.Repositories
{
    public interface IToDoRepository
    {
        Task<ToDo?> GetByIdAsync(Guid id);
        Task<IEnumerable<ToDo>> GetAllAsync();
        Task AddAsync(ToDo todo);
        Task UpdateAsync(ToDo todo);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ChangeStatusAsync(Guid id, bool isCompleted);
    }
}