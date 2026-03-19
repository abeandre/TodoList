using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ToDoApi.Controllers;
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
            var returnedTodos = Assert.IsAssignableFrom<IEnumerable<ToDo.DataAccess.ToDo>>(okResult.Value);
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
            var returnedTodo = Assert.IsType<ToDo.DataAccess.ToDo>(okResult.Value);
            Assert.Equal(todoId, returnedTodo.Id);
        }

        [Fact]
        public async Task GetByIdReturnsNotFoundResultWhenTodoDoesNotExist()
        {
            // Arrange
            _mockRepo.Setup(repo => repo.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ToDo.DataAccess.ToDo)null);

            // Act
            var result = await _controller.GetById(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CreateReturnsCreatedAtActionResultAndSetsDates()
        {
            // Arrange
            var newTodo = new ToDo.DataAccess.ToDo { Title = "Add Me" };
            _mockRepo.Setup(repo => repo.AddAsync(It.IsAny<ToDo.DataAccess.ToDo>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(newTodo);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(ToDoController.GetById), createdResult.ActionName);
            var returnedTodo = Assert.IsType<ToDo.DataAccess.ToDo>(createdResult.Value);
            Assert.NotEqual(Guid.Empty, returnedTodo.Id);
            Assert.NotEqual(default(DateTime), returnedTodo.CreatedAt);
        }

        [Fact]
        public async Task UpdateReturnsBadRequestWhenIdMismatch()
        {
            // Arrange
            var todo = new ToDo.DataAccess.ToDo { Id = Guid.NewGuid(), Title = "Updated" };
            var differentId = Guid.NewGuid();

            // Act
            var result = await _controller.Update(differentId, todo);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task UpdateReturnsOkResultWhenValid()
        {
            // Arrange
            var todoId = Guid.NewGuid();
            var todo = new ToDo.DataAccess.ToDo { Id = todoId, Title = "Updated" };
            _mockRepo.Setup(repo => repo.UpdateAsync(It.IsAny<ToDo.DataAccess.ToDo>())).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Update(todoId, todo);

            // Assert
            Assert.IsType<OkResult>(result);
            _mockRepo.Verify(repo => repo.UpdateAsync(todo), Times.Once);
        }

        [Fact]
        public async Task DeleteReturnsOkResult()
        {
            // Arrange
            var todoId = Guid.NewGuid();
            _mockRepo.Setup(repo => repo.DeleteAsync(todoId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Delete(todoId);

            // Assert
            Assert.IsType<OkResult>(result);
            _mockRepo.Verify(repo => repo.DeleteAsync(todoId), Times.Once);
        }

        [Fact]
        public async Task ChangeStatusReturnsOkResult()
        {
            // Arrange
            var todoId = Guid.NewGuid();
            _mockRepo.Setup(repo => repo.ChangeStatusAsync(todoId, true)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.ChangeStatus(todoId, true);

            // Assert
            Assert.IsType<OkResult>(result);
            _mockRepo.Verify(repo => repo.ChangeStatusAsync(todoId, true), Times.Once);
        }
    }
}
