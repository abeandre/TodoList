using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using ToDo.DataAccess;
using ToDo.DataAccess.Repositories;

namespace ToDo.DataAccess.Tests
{
    public class ToDoRepositoryTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task AddAsyncShouldAddToDo()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new ToDoRepository(context, NullLogger<ToDoRepository>.Instance);
            var todo = new DataAccess.ToDo { Id = Guid.NewGuid(), Title = "Test Add", Description = "Testing Add" };

            // Act
            await repository.AddAsync(todo);

            // Assert
            var result = await context.ToDos.FindAsync(todo.Id);
            Assert.NotNull(result);
            Assert.Equal("Test Add", result!.Title);
        }

        [Fact]
        public async Task GetByIdAsyncShouldReturnCorrectToDo()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new ToDoRepository(context, NullLogger<ToDoRepository>.Instance);
            var id = Guid.NewGuid();
            context.ToDos.Add(new DataAccess.ToDo { Id = id, Title = "Test Get" });
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetByIdAsync(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
            Assert.Equal("Test Get", result.Title);
        }

        [Fact]
        public async Task GetByIdAsyncShouldReturnNullWhenNotFound()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new ToDoRepository(context, NullLogger<ToDoRepository>.Instance);

            // Act
            var result = await repository.GetByIdAsync(Guid.NewGuid());

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsyncShouldReturnAllToDos()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new ToDoRepository(context, NullLogger<ToDoRepository>.Instance);
            context.ToDos.AddRange(
                new DataAccess.ToDo { Id = Guid.NewGuid(), Title = "Task 1" },
                new DataAccess.ToDo { Id = Guid.NewGuid(), Title = "Task 2" }
            );
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task UpdateAsyncShouldUpdateToDo()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new ToDoRepository(context, NullLogger<ToDoRepository>.Instance);
            var todo = new DataAccess.ToDo { Id = Guid.NewGuid(), Title = "Old Title" };
            context.ToDos.Add(todo);
            await context.SaveChangesAsync();

            // Act
            todo.Title = "New Title";
            await repository.UpdateAsync(todo);

            // Assert
            var result = await context.ToDos.FindAsync(todo.Id);
            Assert.NotNull(result);
            Assert.Equal("New Title", result!.Title);
        }

        [Fact]
        public async Task DeleteAsyncShouldRemoveToDoAndReturnTrue()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new ToDoRepository(context, NullLogger<ToDoRepository>.Instance);
            var id = Guid.NewGuid();
            context.ToDos.Add(new DataAccess.ToDo { Id = id, Title = "To Delete" });
            await context.SaveChangesAsync();

            // Act
            var deleted = await repository.DeleteAsync(id);

            // Assert
            Assert.True(deleted);
            var result = await context.ToDos.FindAsync(id);
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteAsyncShouldReturnFalseWhenNotFound()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new ToDoRepository(context, NullLogger<ToDoRepository>.Instance);

            // Act
            var deleted = await repository.DeleteAsync(Guid.NewGuid());

            // Assert
            Assert.False(deleted);
        }

        [Fact]
        public async Task ChangeStatusAsyncShouldSetFinishedAtWhenCompletedAndReturnTrue()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new ToDoRepository(context, NullLogger<ToDoRepository>.Instance);
            var id = Guid.NewGuid();
            context.ToDos.Add(new DataAccess.ToDo { Id = id, Title = "Status Test" });
            await context.SaveChangesAsync();

            // Act
            var changed = await repository.ChangeStatusAsync(id, true);

            // Assert
            Assert.True(changed);
            var result = await context.ToDos.FindAsync(id);
            Assert.NotNull(result);
            Assert.NotNull(result.FinishedAt);
        }

        [Fact]
        public async Task ChangeStatusAsyncShouldNullifyFinishedAtWhenNotCompleted()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new ToDoRepository(context, NullLogger<ToDoRepository>.Instance);
            var id = Guid.NewGuid();
            context.ToDos.Add(new DataAccess.ToDo { Id = id, Title = "Status Test 2", FinishedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            // Act
            var changed = await repository.ChangeStatusAsync(id, false);

            // Assert
            Assert.True(changed);
            var result = await context.ToDos.FindAsync(id);
            Assert.NotNull(result);
            Assert.Null(result.FinishedAt);
        }

        [Fact]
        public async Task ChangeStatusAsyncShouldReturnFalseWhenNotFound()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new ToDoRepository(context, NullLogger<ToDoRepository>.Instance);

            // Act
            var changed = await repository.ChangeStatusAsync(Guid.NewGuid(), true);

            // Assert
            Assert.False(changed);
        }
    }
}
