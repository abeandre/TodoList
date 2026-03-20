using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ToDo.DataAccess;

namespace ToDo.DataAccess.Repositories
{
    public class ToDoRepository : IToDoRepository
    {
        private readonly AppDbContext _context;

        public ToDoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DataAccess.ToDo?> GetByIdAsync(Guid id)
        {
            return await _context.ToDos.FindAsync(id);
        }

        public async Task<IEnumerable<DataAccess.ToDo>> GetAllAsync()
        {
            return await _context.ToDos.ToListAsync();
        }

        public async Task AddAsync(DataAccess.ToDo todo)
        {
            await _context.ToDos.AddAsync(todo);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(DataAccess.ToDo todo)
        {
            _context.ToDos.Update(todo);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var todo = await GetByIdAsync(id);
            if (todo == null)
                return false;

            _context.ToDos.Remove(todo);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangeStatusAsync(Guid id, bool isCompleted)
        {
            var todo = await GetByIdAsync(id);
            if (todo == null)
                return false;

            todo.FinishedAt = isCompleted ? DateTime.UtcNow : null;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
