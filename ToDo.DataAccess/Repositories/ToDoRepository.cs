using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ToDo.DataAccess;

namespace ToDo.DataAccess.Repositories
{
    public class ToDoRepository : IToDoRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ToDoRepository> _logger;

        public ToDoRepository(AppDbContext context, ILogger<ToDoRepository> logger)
        {
            _context = context;
            _logger = logger;
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
            _logger.LogDebug("Persisted new todo {Id}", todo.Id);
        }

        public async Task UpdateAsync(DataAccess.ToDo todo)
        {
            _context.ToDos.Update(todo);
            await _context.SaveChangesAsync();
            _logger.LogDebug("Persisted update for todo {Id}", todo.Id);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var todo = await _context.ToDos.FindAsync(id);
            if (todo is null)
            {
                _logger.LogDebug("DeleteAsync: todo {Id} not found", id);
                return false;
            }
            _context.ToDos.Remove(todo);
            await _context.SaveChangesAsync();
            _logger.LogDebug("Deleted todo {Id}", id);
            return true;
        }

        public async Task<bool> ChangeStatusAsync(Guid id, bool isCompleted)
        {
            var todo = await GetByIdAsync(id);
            if (todo == null)
            {
                _logger.LogDebug("ChangeStatusAsync: todo {Id} not found", id);
                return false;
            }

            todo.FinishedAt = isCompleted ? DateTime.UtcNow : null;
            todo.UpdatedAt = DateTime.UtcNow;
            _context.ToDos.Update(todo);
            await _context.SaveChangesAsync();
            _logger.LogDebug("Todo {Id} status changed to {Status}", id, isCompleted ? "completed" : "active");
            return true;
        }
    }
}
