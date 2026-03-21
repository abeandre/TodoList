using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
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
        private readonly Guid _userId = Guid.NewGuid();

        public ToDoControllerTests()
        {
            _mockService = new Mock<IToDoService>();
            
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, _userId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller = new ToDoController(_mockService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = claimsPrincipal }
                }
            };
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
            _mockService.Setup(s => s.GetAllAsync(_userId)).ReturnsAsync(todos);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<ToDoResponse>>(okResult.Value);
            Assert.Equal(2, returned.Count());
        }

        [Fact]
        public async Task CreateReturnsCreatedAtActionResult()
        {
            // Arrange
            var request = new CreateToDoRequest { Title = "Add Me" };
            var created = new ToDoResponse { Id = Guid.NewGuid(), Title = "Add Me", CreatedAt = DateTime.UtcNow };
            _mockService.Setup(s => s.CreateAsync(_userId, request)).ReturnsAsync(created);

            // Act
            var result = await _controller.Create(request);

            // Assert
            var createdResult = Assert.IsType<CreatedResult>(result.Result);
            var returned = Assert.IsType<ToDoResponse>(createdResult.Value);
            Assert.Equal(created.Id, returned.Id);
        }

        [Fact]
        public async Task UpdateReturnsNoContentWhenFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            var request = new UpdateToDoRequest { Title = "Updated" };
            _mockService.Setup(s => s.UpdateAsync(id, _userId, request)).ReturnsAsync(true);

            // Act
            var result = await _controller.Update(id, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockService.Verify(s => s.UpdateAsync(id, _userId, request), Times.Once);
        }

        [Fact]
        public async Task UpdateReturnsNotFoundWhenTodoDoesNotExist()
        {
            // Arrange
            _mockService.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), _userId, It.IsAny<UpdateToDoRequest>())).ReturnsAsync(false);

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
            _mockService.Setup(s => s.DeleteAsync(id, _userId)).ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockService.Verify(s => s.DeleteAsync(id, _userId), Times.Once);
        }

        [Fact]
        public async Task DeleteReturnsNotFoundWhenTodoDoesNotExist()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAsync(It.IsAny<Guid>(), _userId)).ReturnsAsync(false);

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
            _mockService.Setup(s => s.ChangeStatusAsync(id, _userId, true)).ReturnsAsync(true);

            // Act
            var result = await _controller.ChangeStatus(id, new ChangeStatusRequest { IsCompleted = true });

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockService.Verify(s => s.ChangeStatusAsync(id, _userId, true), Times.Once);
        }

        [Fact]
        public async Task ChangeStatusReturnsNotFoundWhenTodoDoesNotExist()
        {
            // Arrange
            _mockService.Setup(s => s.ChangeStatusAsync(It.IsAny<Guid>(), _userId, It.IsAny<bool>())).ReturnsAsync(false);

            // Act
            var result = await _controller.ChangeStatus(Guid.NewGuid(), new ChangeStatusRequest { IsCompleted = false });

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
