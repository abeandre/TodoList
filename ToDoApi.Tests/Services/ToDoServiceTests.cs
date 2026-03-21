using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ToDoServiceTests
    {
        private readonly Mock<IToDoRepository> _mockRepo;
        private readonly IMapper _mapper;
        private readonly ToDoService _service;
        private readonly Guid _userId = Guid.NewGuid();

        public ToDoServiceTests()
        {
            _mockRepo = new Mock<IToDoRepository>();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAutoMapper(cfg => cfg.AddProfile<ToDoMappingProfile>());
            _mapper = services.BuildServiceProvider().GetRequiredService<IMapper>();
            _service = new ToDoService(_mockRepo.Object, _mapper, NullLogger<ToDoService>.Instance);
        }

        // --- GetAllAsync ---

        [Fact]
        public async Task GetAllAsyncReturnsMappedResponses()
        {
            var todos = new List<ToDo.DataAccess.ToDo>
            {
                new() { Id = Guid.NewGuid(), Title = "First", Description = "Desc 1", CreatedAt = DateTime.UtcNow },
                new() { Id = Guid.NewGuid(), Title = "Second", Description = "Desc 2", CreatedAt = DateTime.UtcNow }
            };
            _mockRepo.Setup(r => r.GetAllAsync(_userId)).ReturnsAsync(todos);

            var result = (await _service.GetAllAsync(_userId)).ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal(todos[0].Id, result[0].Id);
            Assert.Equal(todos[0].Title, result[0].Title);
            Assert.Equal(todos[1].Id, result[1].Id);
        }

        [Fact]
        public async Task GetAllAsyncReturnsEmptyWhenNoTodos()
        {
            _mockRepo.Setup(r => r.GetAllAsync(_userId)).ReturnsAsync(new List<ToDo.DataAccess.ToDo>());

            var result = await _service.GetAllAsync(_userId);

            Assert.Empty(result);
        }

        // --- CreateAsync ---

        [Fact]
        public async Task CreateAsyncMapsFieldsAndPersists()
        {
            var request = new CreateToDoRequest { Title = "New Todo", Description = "Some desc" };
            _mockRepo.Setup(r => r.AddAsync(It.IsAny<ToDo.DataAccess.ToDo>())).Returns(Task.CompletedTask);

            var result = await _service.CreateAsync(_userId, request);

            Assert.Equal("New Todo", result.Title);
            Assert.Equal("Some desc", result.Description);
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<ToDo.DataAccess.ToDo>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsyncAssignsNewId()
        {
            var request = new CreateToDoRequest { Title = "Todo" };
            _mockRepo.Setup(r => r.AddAsync(It.IsAny<ToDo.DataAccess.ToDo>())).Returns(Task.CompletedTask);

            var result = await _service.CreateAsync(_userId, request);

            Assert.NotEqual(Guid.Empty, result.Id);
        }

        [Fact]
        public async Task CreateAsyncAssignsCreatedAtAndUpdatedAt()
        {
            var before = DateTime.UtcNow;
            var request = new CreateToDoRequest { Title = "Todo" };
            _mockRepo.Setup(r => r.AddAsync(It.IsAny<ToDo.DataAccess.ToDo>())).Returns(Task.CompletedTask);

            var result = await _service.CreateAsync(_userId, request);

            Assert.True(result.CreatedAt >= before);
            Assert.True(result.CreatedAt <= DateTime.UtcNow);
            Assert.True(result.UpdatedAt >= before);
            Assert.True(result.UpdatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public async Task CreateAsyncDefaultsDescriptionToEmpty()
        {
            var request = new CreateToDoRequest { Title = "Todo" };
            _mockRepo.Setup(r => r.AddAsync(It.IsAny<ToDo.DataAccess.ToDo>())).Returns(Task.CompletedTask);

            var result = await _service.CreateAsync(_userId, request);

            Assert.Equal(string.Empty, result.Description);
        }

        // --- UpdateAsync ---

        [Fact]
        public async Task UpdateAsyncReturnsFalse_WhenNotFound()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _userId)).ReturnsAsync((ToDo.DataAccess.ToDo?)null);

            var result = await _service.UpdateAsync(Guid.NewGuid(), _userId, new UpdateToDoRequest { Title = "x" });

            Assert.False(result);
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<ToDo.DataAccess.ToDo>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsyncReturnsTrue_AndPersistsChanges()
        {
            var id = Guid.NewGuid();
            var existing = new ToDo.DataAccess.ToDo { Id = id, Title = "Old", Description = "Old desc" };
            var request = new UpdateToDoRequest { Title = "New", Description = "New desc" };
            _mockRepo.Setup(r => r.GetByIdAsync(id, _userId)).ReturnsAsync(existing);
            _mockRepo.Setup(r => r.UpdateAsync(existing)).Returns(Task.CompletedTask);

            var result = await _service.UpdateAsync(id, _userId, request);

            Assert.True(result);
            Assert.Equal("New", existing.Title);
            Assert.Equal("New desc", existing.Description);
            _mockRepo.Verify(r => r.UpdateAsync(existing), Times.Once);
        }

        [Fact]
        public async Task UpdateAsyncSetsUpdatedAt()
        {
            var before = DateTime.UtcNow;
            var id = Guid.NewGuid();
            var existing = new ToDo.DataAccess.ToDo { Id = id, Title = "Old" };
            _mockRepo.Setup(r => r.GetByIdAsync(id, _userId)).ReturnsAsync(existing);
            _mockRepo.Setup(r => r.UpdateAsync(existing)).Returns(Task.CompletedTask);

            await _service.UpdateAsync(id, _userId, new UpdateToDoRequest { Title = "New" });

            Assert.True(existing.UpdatedAt >= before);
            Assert.True(existing.UpdatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public async Task UpdateAsyncPreservesIdAndCreatedAt()
        {
            var id = Guid.NewGuid();
            var createdAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var existing = new ToDo.DataAccess.ToDo { Id = id, Title = "Old", CreatedAt = createdAt };
            _mockRepo.Setup(r => r.GetByIdAsync(id, _userId)).ReturnsAsync(existing);
            _mockRepo.Setup(r => r.UpdateAsync(existing)).Returns(Task.CompletedTask);

            await _service.UpdateAsync(id, _userId, new UpdateToDoRequest { Title = "New" });

            Assert.Equal(id, existing.Id);
            Assert.Equal(createdAt, existing.CreatedAt);
        }

        // --- DeleteAsync ---

        [Fact]
        public async Task DeleteAsyncReturnsTrue_WhenFound()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.DeleteAsync(id, _userId)).ReturnsAsync(true);

            var result = await _service.DeleteAsync(id, _userId);

            Assert.True(result);
            _mockRepo.Verify(r => r.DeleteAsync(id, _userId), Times.Once);
        }

        [Fact]
        public async Task DeleteAsyncReturnsFalse_WhenNotFound()
        {
            _mockRepo.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), _userId)).ReturnsAsync(false);

            var result = await _service.DeleteAsync(Guid.NewGuid(), _userId);

            Assert.False(result);
        }

        // --- Exception propagation ---

        [Fact]
        public async Task GetAllAsyncPropagatesRepositoryException()
        {
            _mockRepo.Setup(r => r.GetAllAsync(_userId)).ThrowsAsync(new InvalidOperationException("DB error"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetAllAsync(_userId));
        }

        [Fact]
        public async Task CreateAsyncPropagatesRepositoryException()
        {
            _mockRepo.Setup(r => r.AddAsync(It.IsAny<ToDo.DataAccess.ToDo>())).ThrowsAsync(new InvalidOperationException("DB error"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(_userId, new CreateToDoRequest { Title = "Test" }));
        }

        [Fact]
        public async Task UpdateAsyncPropagatesRepositoryException()
        {
            var id = Guid.NewGuid();
            var existing = new ToDo.DataAccess.ToDo { Id = id, Title = "Old" };
            _mockRepo.Setup(r => r.GetByIdAsync(id, _userId)).ReturnsAsync(existing);
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<ToDo.DataAccess.ToDo>())).ThrowsAsync(new InvalidOperationException("DB error"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(id, _userId, new UpdateToDoRequest { Title = "New" }));
        }

        [Fact]
        public async Task DeleteAsyncPropagatesRepositoryException()
        {
            _mockRepo.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), _userId)).ThrowsAsync(new InvalidOperationException("DB error"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(Guid.NewGuid(), _userId));
        }

        [Fact]
        public async Task ChangeStatusAsyncPropagatesRepositoryException()
        {
            _mockRepo.Setup(r => r.ChangeStatusAsync(It.IsAny<Guid>(), _userId, It.IsAny<bool>())).ThrowsAsync(new InvalidOperationException("DB error"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ChangeStatusAsync(Guid.NewGuid(), _userId, true));
        }

        // --- ChangeStatusAsync ---

        [Fact]
        public async Task ChangeStatusAsyncReturnsTrue_WhenFound()
        {
            var id = Guid.NewGuid();
            _mockRepo.Setup(r => r.ChangeStatusAsync(id, _userId, true)).ReturnsAsync(true);

            var result = await _service.ChangeStatusAsync(id, _userId, true);

            Assert.True(result);
            _mockRepo.Verify(r => r.ChangeStatusAsync(id, _userId, true), Times.Once);
        }

        [Fact]
        public async Task ChangeStatusAsyncReturnsFalse_WhenNotFound()
        {
            _mockRepo.Setup(r => r.ChangeStatusAsync(It.IsAny<Guid>(), _userId, It.IsAny<bool>())).ReturnsAsync(false);

            var result = await _service.ChangeStatusAsync(Guid.NewGuid(), _userId, false);

            Assert.False(result);
        }
    }
}
