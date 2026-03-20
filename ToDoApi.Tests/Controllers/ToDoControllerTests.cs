using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ToDoApi.Controllers;
using ToDoApi.Models;
using ToDo.DataAccess.Repositories;
using Xunit;

namespace ToDoApi.Tests.Controllers
{
    public class ToDoControllerTests
    {
        private readonly Mock<IToDoRepository> _mockRepo;
        private readonly ToDoController _controller;

        public ToDoControllerTests()
        {
            _mockRepo = new Mock<IToDoRepository>();
            _controller = new ToDoController(_mockRepo.Object);
        }

        [Fact]
        public async Task GetAllReturnsOkObjectResultWithListOfTodos()
        {
            // Arrange
            var mockTodos = new List<ToDo.DataAccess.ToDo>
            {
                new ToDo.DataAccess.ToDo { Id = Guid.NewGuid(), Title = "Test 1" },
                new ToDo.DataAccess.ToDo { Id = Guid.NewGuid(), Title = "Test 2" }
            };
            _mockRepo.Setup(repo => repo.GetAllAsync()).ReturnsAsync(mockTodos);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedTodos = Assert.IsAssignableFrom<IEnumerable<ToDoResponse>>(okResult.Value);
            Assert.Equal(2, returnedTodos.Count());
        }

        [Fact]
        public async Task GetByIdReturnsOkObjectResultWhenTodoExists()
        {
            // Arrange
            var todoId = Guid.NewGuid();
            var mockTodo = new ToDo.DataAccess.ToDo { Id = todoId, Title = "Found It" };
            _mockRepo.Setup(repo => repo.GetByIdAsync(todoId)).ReturnsAsync(mockTodo);

            // Act
            var result = await _controller.GetById(todoId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedTodo = Assert.IsType<ToDoResponse>(okResult.Value);
            Assert.Equal(todoId, returnedTodo.Id);
        }

        [Fact]
        public async Task GetByIdReturnsNotFoundResultWhenTodoDoesNotExist()
        {
            // Arrange
            _mockRepo.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ToDo.DataAccess.ToDo?)null);

            // Act
            var result = await _controller.GetById(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CreateReturnsCreatedAtActionResultAndSetsDates()
        {
            // Arrange
            var request = new CreateToDoRequest { Title = "Add Me" };
            _mockRepo.Setup(repo => repo.AddAsync(It.IsAny<ToDo.DataAccess.ToDo>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(ToDoController.GetById), createdResult.ActionName);
            var returnedTodo = Assert.IsType<ToDoResponse>(createdResult.Value);
            Assert.NotEqual(Guid.Empty, returnedTodo.Id);
            Assert.NotEqual(default(DateTime), returnedTodo.CreatedAt);
        }

        [Fact]
        public async Task UpdateReturnsNotFoundWhenTodoDoesNotExist()
        {
            // Arrange
            _mockRepo.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ToDo.DataAccess.ToDo?)null);
            var request = new UpdateToDoRequest { Title = "Updated" };

            // Act
            var result = await _controller.Update(Guid.NewGuid(), request);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateReturnsNoContentWhenValid()
        {
            // Arrange
            var todoId = Guid.NewGuid();
            var existing = new ToDo.DataAccess.ToDo { Id = todoId, Title = "Old Title" };
            var request = new UpdateToDoRequest { Title = "Updated" };
            _mockRepo.Setup(repo => repo.GetByIdAsync(todoId)).ReturnsAsync(existing);
            _mockRepo.Setup(repo => repo.UpdateAsync(It.IsAny<ToDo.DataAccess.ToDo>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Update(todoId, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockRepo.Verify(repo => repo.UpdateAsync(existing), Times.Once);
        }

        [Fact]
        public async Task DeleteReturnsNoContentWhenFound()
        {
            // Arrange
            var todoId = Guid.NewGuid();
            _mockRepo.Setup(repo => repo.DeleteAsync(todoId)).ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(todoId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockRepo.Verify(repo => repo.DeleteAsync(todoId), Times.Once);
        }

        [Fact]
        public async Task DeleteReturnsNotFoundWhenTodoDoesNotExist()
        {
            // Arrange
            _mockRepo.Setup(repo => repo.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ChangeStatusReturnsNoContentWhenFound()
        {
            // Arrange
            var todoId = Guid.NewGuid();
            _mockRepo.Setup(repo => repo.ChangeStatusAsync(todoId, true)).ReturnsAsync(true);

            // Act
            var result = await _controller.ChangeStatus(todoId, true);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockRepo.Verify(repo => repo.ChangeStatusAsync(todoId, true), Times.Once);
        }

        [Fact]
        public async Task ChangeStatusReturnsNotFoundWhenTodoDoesNotExist()
        {
            // Arrange
            _mockRepo.Setup(repo => repo.ChangeStatusAsync(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync(false);

            // Act
            var result = await _controller.ChangeStatus(Guid.NewGuid(), true);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
