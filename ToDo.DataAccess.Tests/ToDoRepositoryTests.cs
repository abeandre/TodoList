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
        private readonly Guid _userId = Guid.NewGuid();
        private static AppDbContext GetInMemoryDbContext(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new AppDbContext(options);
        }

        private sealed class ThrowingDbContext : AppDbContext
        {
            public ThrowingDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
                => throw new DbUpdateException("Simulated save failure.", new Exception());

            public Task<int> BaseSaveChangesAsync(CancellationToken cancellationToken = default)
                => base.SaveChangesAsync(cancellationToken);
        }

        private static ThrowingDbContext GetThrowingDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new ThrowingDbContext(options);
        }

        [Fact]
        public async Task AddAsyncShouldAddToDo()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new ToDoRepository(context, NullLogger<ToDoRepository>.Instance);
            var todo = new DataAccess.ToDo { Id = Guid.NewGuid(), UserId = _userId, Title = "Test Add", Description = "Testing Add" };

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
            context.ToDos.Add(new DataAccess.ToDo { Id = id, UserId = _userId, Title = "Test Get" });
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetByIdAsync(id, _userId);

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
            var result = await repository.GetByIdAsync(Guid.NewGuid(), _userId);

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
                new DataAccess.ToDo { Id = Guid.NewGuid(), UserId = _userId, Title = "Task 1" },
                new DataAccess.ToDo { Id = Guid.NewGuid(), UserId = _userId, Title = "Task 2" }
            );
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllAsync(_userId);

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task UpdateAsyncShouldUpdateToDo()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new ToDoRepository(context, NullLogger<ToDoRepository>.Instance);
            var todo = new DataAccess.ToDo { Id = Guid.NewGuid(), UserId = _userId, Title = "Old Title" };
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
            context.ToDos.Add(new DataAccess.ToDo { Id = id, UserId = _userId, Title = "To Delete" });
            await context.SaveChangesAsync();

            // Act
            var deleted = await repository.DeleteAsync(id, _userId);

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
            var deleted = await repository.DeleteAsync(Guid.NewGuid(), _userId);

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
            context.ToDos.Add(new DataAccess.ToDo { Id = id, UserId = _userId, Title = "Status Test" });
            await context.SaveChangesAsync();

            // Act
            var changed = await repository.ChangeStatusAsync(id, _userId, true);

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
            context.ToDos.Add(new DataAccess.ToDo { Id = id, UserId = _userId, Title = "Status Test 2", FinishedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            // Act
            var changed = await repository.ChangeStatusAsync(id, _userId, false);

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
            var changed = await repository.ChangeStatusAsync(Guid.NewGuid(), _userId, true);

            // Assert
            Assert.False(changed);
        }

        [Fact]
        public async Task AddAsync_PropagatesException_WhenSaveChangesFails()
        {
            var context = GetThrowingDbContext();
            var repository = new ToDoRepository(context, NullLogger<ToDoRepository>.Instance);
            var todo = new DataAccess.ToDo { Id = Guid.NewGuid(), UserId = _userId, Title = "Fail" };

            await Assert.ThrowsAsync<DbUpdateException>(() => repository.AddAsync(todo));
        }

        [Fact]
        public async Task UpdateAsync_PropagatesException_WhenSaveChangesFails()
        {
            var context = GetThrowingDbContext();
            var todo = new DataAccess.ToDo { Id = Guid.NewGuid(), UserId = _userId, Title = "Original" };
            context.ToDos.Add(todo);
            await context.BaseSaveChangesAsync();

            var repository = new ToDoRepository(context, NullLogger<ToDoRepository>.Instance);
            todo.Title = "Updated";

            await Assert.ThrowsAsync<DbUpdateException>(() => repository.UpdateAsync(todo));
        }

        [Fact]
        public async Task ChangeStatusAsync_PropagatesException_WhenSaveChangesFails()
        {
            var context = GetThrowingDbContext();
            var id = Guid.NewGuid();
            context.ToDos.Add(new DataAccess.ToDo { Id = id, UserId = _userId, Title = "Status Fail" });
            await context.BaseSaveChangesAsync();

            var repository = new ToDoRepository(context, NullLogger<ToDoRepository>.Instance);

            await Assert.ThrowsAsync<DbUpdateException>(() => repository.ChangeStatusAsync(id, _userId, true));
        }

        [Fact]
        public async Task DeleteAsync_ReturnsFalse_WhenEntityAlreadyDeletedByConcurrentContext()
        {
            var dbName = Guid.NewGuid().ToString();
            var contextA = GetInMemoryDbContext(dbName);
            var contextB = GetInMemoryDbContext(dbName);
            var repoA = new ToDoRepository(contextA, NullLogger<ToDoRepository>.Instance);
            var repoB = new ToDoRepository(contextB, NullLogger<ToDoRepository>.Instance);

            var id = Guid.NewGuid();
            await repoA.AddAsync(new DataAccess.ToDo { Id = id, UserId = _userId, Title = "Concurrent" });

            var firstDelete = await repoA.DeleteAsync(id, _userId);
            var secondDelete = await repoB.DeleteAsync(id, _userId);

            Assert.True(firstDelete);
            Assert.False(secondDelete);
        }
    }
}
