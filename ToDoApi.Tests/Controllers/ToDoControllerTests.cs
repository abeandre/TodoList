using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ToDoApi.Controllers;
using ToDoApi.Models;
using ToDoApi.Services;
using Xunit;

namespace ToDoApi.Tests.Controllers
{
    public class ToDoControllerTests
    {
        private readonly Mock<IToDoService> _mockService;
        private readonly ToDoController _controller;

        public ToDoControllerTests()
        {
            _mockService = new Mock<IToDoService>();
            _controller = new ToDoController(_mockService.Object);
        }

        [Fact]
        public async Task GetAllReturnsOkObjectResultWithListOfTodos()
        {
            // Arrange
            var todos = new List<ToDoResponse>
            {
                new() { Id = Guid.NewGuid(), Title = "Test 1" },
                new() { Id = Guid.NewGuid(), Title = "Test 2" }
            };
            _mockService.Setup(s => s.GetAllAsync()).ReturnsAsync(todos);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<ToDoResponse>>(okResult.Value);
            Assert.Equal(2, returned.Count());
        }

        [Fact]
        public async Task GetByIdReturnsOkObjectResultWhenTodoExists()
        {
            // Arrange
            var id = Guid.NewGuid();
            var todo = new ToDoResponse { Id = id, Title = "Found It" };
            _mockService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(todo);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<ToDoResponse>(okResult.Value);
            Assert.Equal(id, returned.Id);
        }

        [Fact]
        public async Task GetByIdReturnsNotFoundWhenTodoDoesNotExist()
        {
            // Arrange
            _mockService.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ToDoResponse?)null);

            // Act
            var result = await _controller.GetById(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CreateReturnsCreatedAtActionResult()
        {
            // Arrange
            var request = new CreateToDoRequest { Title = "Add Me" };
            var created = new ToDoResponse { Id = Guid.NewGuid(), Title = "Add Me", CreatedAt = DateTime.UtcNow };
            _mockService.Setup(s => s.CreateAsync(request)).ReturnsAsync(created);

            // Act
            var result = await _controller.Create(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(ToDoController.GetById), createdResult.ActionName);
            var returned = Assert.IsType<ToDoResponse>(createdResult.Value);
            Assert.Equal(created.Id, returned.Id);
        }

        [Fact]
        public async Task UpdateReturnsNoContentWhenFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            var request = new UpdateToDoRequest { Title = "Updated" };
            _mockService.Setup(s => s.UpdateAsync(id, request)).ReturnsAsync(true);

            // Act
            var result = await _controller.Update(id, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockService.Verify(s => s.UpdateAsync(id, request), Times.Once);
        }

        [Fact]
        public async Task UpdateReturnsNotFoundWhenTodoDoesNotExist()
        {
            // Arrange
            _mockService.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateToDoRequest>())).ReturnsAsync(false);

            // Act
            var result = await _controller.Update(Guid.NewGuid(), new UpdateToDoRequest { Title = "x" });

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteReturnsNoContentWhenFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockService.Setup(s => s.DeleteAsync(id)).ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockService.Verify(s => s.DeleteAsync(id), Times.Once);
        }

        [Fact]
        public async Task DeleteReturnsNotFoundWhenTodoDoesNotExist()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(Guid.NewGuid());

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ChangeStatusReturnsNoContentWhenFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _mockService.Setup(s => s.ChangeStatusAsync(id, true)).ReturnsAsync(true);

            // Act
            var result = await _controller.ChangeStatus(id, true);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockService.Verify(s => s.ChangeStatusAsync(id, true), Times.Once);
        }

        [Fact]
        public async Task ChangeStatusReturnsNotFoundWhenTodoDoesNotExist()
        {
            // Arrange
            _mockService.Setup(s => s.ChangeStatusAsync(It.IsAny<Guid>(), It.IsAny<bool>())).ReturnsAsync(false);

            // Act
            var result = await _controller.ChangeStatus(Guid.NewGuid(), false);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
