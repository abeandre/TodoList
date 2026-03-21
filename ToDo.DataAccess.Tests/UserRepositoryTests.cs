using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using ToDo.DataAccess;
using ToDo.DataAccess.Repositories;

namespace ToDo.DataAccess.Tests
{
    public class UserRepositoryTests
    {
        private static AppDbContext GetInMemoryDbContext(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task CreateUserAsync_ShouldAddNewUser()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new UserRepository(context);
            var user = new User 
            { 
                Id = Guid.NewGuid(), 
                Name = "John Doe", 
                Email = "john@example.com", 
                Salt = "salt123", 
                HashedPassword = "hashedpassword123",
                CreatedDate = DateTime.UtcNow,
                LastModifiedDate = DateTime.UtcNow
            };

            // Act
            var createdUser = await repository.CreateUserAsync(user);

            // Assert
            var result = await context.Users.FindAsync(user.Id);
            Assert.NotNull(result);
            Assert.Equal("john@example.com", result!.Email);
            Assert.Equal("John Doe", result.Name);
            Assert.Equal(createdUser.Id, result.Id);
        }

        [Fact]
        public async Task GetUserByEmailAsync_ShouldReturnCorrectUser()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new UserRepository(context);
            var email = "jane@example.com";
            var user = new User 
            { 
                Id = Guid.NewGuid(), 
                Name = "Jane Doe", 
                Email = email, 
                Salt = "salt456", 
                HashedPassword = "hashedpassword456" 
            };
            
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetUserByEmailAsync(email);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(email, result!.Email);
            Assert.Equal("Jane Doe", result.Name);
        }

        [Fact]
        public async Task GetUserByEmailAsync_ShouldReturnNullWhenNotFound()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new UserRepository(context);

            // Act
            var result = await repository.GetUserByEmailAsync("nonexistent@example.com");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateUserAsync_ShouldUpdateExistingUser()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var repository = new UserRepository(context);
            var user = new User 
            { 
                Id = Guid.NewGuid(), 
                Name = "Old Name", 
                Email = "old@example.com",
                Salt = "oldsalt",
                HashedPassword = "oldpassword"
            };
            
            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Act
            user.Name = "New Name";
            user.Email = "new@example.com";
            var updatedUser = await repository.UpdateUserAsync(user);

            // Assert
            var result = await context.Users.FindAsync(user.Id);
            Assert.NotNull(result);
            Assert.Equal("New Name", result!.Name);
            Assert.Equal("new@example.com", result.Email);
            Assert.Equal(updatedUser.Id, result.Id);
        }
    }
}
