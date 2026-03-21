using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ToDo.DataAccess.Repositories;
using ToDoApi.Mappings;
using ToDoApi.Models;
using ToDoApi.Services;
using Xunit;

namespace ToDoApi.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _mockRepo;
        private readonly IMapper _mapper;
        private readonly UserService _service;

        public UserServiceTests()
        {
            _mockRepo = new Mock<IUserRepository>();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg => cfg.AddProfile<UserMappingProfile>());
            _mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();
            _service = new UserService(_mockRepo.Object, _mapper, NullLogger<UserService>.Instance);
        }

        [Fact]
        public async Task CreateAsyncMapsFieldsGeneratesSaltAndPersists()
        {
            // Arrange
            var request = new CreateUserRequest { Name = "Test User", Email = "test@example.com", Password = "mypassword" };
            _mockRepo.Setup(r => r.CreateUserAsync(It.IsAny<ToDo.DataAccess.User>())).ReturnsAsync((ToDo.DataAccess.User u) => u);

            // Act
            var result = await _service.CreateAsync(request);

            // Assert
            Assert.Equal("Test User", result.Name);
            Assert.Equal("test@example.com", result.Email);
            Assert.NotEqual(Guid.Empty, result.Id);
            _mockRepo.Verify(r => r.CreateUserAsync(It.Is<ToDo.DataAccess.User>(u => 
                u.Name == "Test User" && 
                !string.IsNullOrEmpty(u.Salt) && 
                !string.IsNullOrEmpty(u.HashedPassword) && 
                u.Id == result.Id
            )), Times.Once);
        }

        [Fact]
        public async Task CreateAsyncThrowsArgumentExceptionWhenEmailExists()
        {
            // Arrange
            var request = new CreateUserRequest { Name = "Test User", Email = "test@example.com", Password = "mypassword" };
            _mockRepo.Setup(r => r.GetUserByEmailAsync("test@example.com")).ReturnsAsync(new ToDo.DataAccess.User { Id = Guid.NewGuid() });

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(request));
            Assert.Equal("Email is already registered", ex.Message);
            _mockRepo.Verify(r => r.CreateUserAsync(It.IsAny<ToDo.DataAccess.User>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsyncReturnsFalse_WhenNotFound()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetUserByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ToDo.DataAccess.User?)null);

            // Act
            var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateUserRequest { Name = "new" });

            // Assert
            Assert.False(result);
            _mockRepo.Verify(r => r.UpdateUserAsync(It.IsAny<ToDo.DataAccess.User>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsyncReturnsTrue_AndPersistsChanges()
        {
            // Arrange
            var id = Guid.NewGuid();
            var existing = new ToDo.DataAccess.User { Id = id, Name = "Old", Email = "old@example.com", Salt = "oldsalt", HashedPassword = "oldhash" };
            var request = new UpdateUserRequest { Name = "New Name", Email = "new@example.com" };
            
            _mockRepo.Setup(r => r.GetUserByIdAsync(id)).ReturnsAsync(existing);
            _mockRepo.Setup(r => r.UpdateUserAsync(existing)).ReturnsAsync(existing);

            // Act
            var result = await _service.UpdateAsync(id, request);

            // Assert
            Assert.True(result);
            Assert.Equal("New Name", existing.Name);
            Assert.Equal("new@example.com", existing.Email);
            Assert.Equal("oldsalt", existing.Salt); // Password wasn't provided, hash should not change
            _mockRepo.Verify(r => r.UpdateUserAsync(existing), Times.Once);
        }

        [Fact]
        public async Task UpdateAsyncUpdatesPassword_WhenPasswordProvided()
        {
            // Arrange
            var id = Guid.NewGuid();
            var existing = new ToDo.DataAccess.User { Id = id, Name = "Old", Email = "old@example.com", Salt = "oldsalt", HashedPassword = "oldhash" };
            var request = new UpdateUserRequest { Name = "New", Email = "new@example.com", Password = "newpassword" };
            
            _mockRepo.Setup(r => r.GetUserByIdAsync(id)).ReturnsAsync(existing);
            _mockRepo.Setup(r => r.UpdateUserAsync(existing)).ReturnsAsync(existing);

            // Act
            var result = await _service.UpdateAsync(id, request);

            // Assert
            Assert.True(result);
            Assert.NotEqual("oldsalt", existing.Salt);
            Assert.NotEqual("oldhash", existing.HashedPassword);
        }

        [Fact]
        public async Task CreateAsyncPropagatesRepositoryException()
        {
            _mockRepo.Setup(r => r.CreateUserAsync(It.IsAny<ToDo.DataAccess.User>())).ThrowsAsync(new InvalidOperationException("DB error"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(new CreateUserRequest { Name = "Test" }));
        }

        [Fact]
        public async Task UpdateAsyncPropagatesRepositoryException()
        {
            var id = Guid.NewGuid();
            var existing = new ToDo.DataAccess.User { Id = id, Name = "Old" };
            _mockRepo.Setup(r => r.GetUserByIdAsync(id)).ReturnsAsync(existing);
            _mockRepo.Setup(r => r.UpdateUserAsync(It.IsAny<ToDo.DataAccess.User>())).ThrowsAsync(new InvalidOperationException("DB error"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(id, new UpdateUserRequest { Name = "New" }));
        }
    }
}
