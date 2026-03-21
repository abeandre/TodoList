using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ToDoApi.Controllers;
using ToDoApi.Models;
using ToDoApi.Services;
using Xunit;

namespace ToDoApi.Tests.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _mockService;
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            _mockService = new Mock<IUserService>();
            _mockAuthService = new Mock<IAuthService>();
            _controller = new UserController(_mockService.Object, _mockAuthService.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        private static ControllerContext MakeContext(Guid userId)
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
            return new ControllerContext { HttpContext = new DefaultHttpContext { User = principal } };
        }

        [Fact]
        public async Task CreateReturnsCreatedAtActionResult_WithUserResponse()
        {
            // Arrange
            var request = new CreateUserRequest { Name = "John", Email = "john@example.com", Password = "pw" };
            var created = new UserResponse { Id = Guid.NewGuid(), Name = "John", Email = "john@example.com" };
            _mockService.Setup(s => s.CreateAsync(request)).ReturnsAsync(created);
            _mockAuthService.Setup(a => a.GenerateToken(created.Id, created.Name, created.Email)).Returns("dummy.jwt.token");

            // Act
            var result = await _controller.Create(request);

            // Assert
            var createdResult = Assert.IsType<CreatedResult>(result.Result);
            var returned = Assert.IsType<UserResponse>(createdResult.Value);
            Assert.Equal(created.Id, returned.Id);
            // JWT is delivered via httpOnly cookie, not the response body
            Assert.True(_controller.HttpContext.Response.Headers.ContainsKey("Set-Cookie"));
        }

        [Fact]
        public async Task CreateReturnsBadRequestWhenArgumentExceptionThrown()
        {
            // Arrange
            var request = new CreateUserRequest { Name = "Duplicate", Email = "dup@ex.com", Password = "pw" };
            _mockService.Setup(s => s.CreateAsync(request)).ThrowsAsync(new ArgumentException("Email is already registered"));

            // Act
            var result = await _controller.Create(request);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        }

        [Fact]
        public async Task UpdateReturnsNoContentWhenFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            _controller.ControllerContext = MakeContext(id);
            var request = new UpdateUserRequest { Name = "Jane", Email = "jane@example.com" };
            _mockService.Setup(s => s.UpdateAsync(id, request)).ReturnsAsync(true);

            // Act
            var result = await _controller.Update(id, request);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockService.Verify(s => s.UpdateAsync(id, request), Times.Once);
        }

        [Fact]
        public async Task UpdateReturnsNotFoundWhenUserDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            _controller.ControllerContext = MakeContext(id);
            _mockService.Setup(s => s.UpdateAsync(id, It.IsAny<UpdateUserRequest>())).ReturnsAsync(false);

            // Act
            var result = await _controller.Update(id, new UpdateUserRequest { Name = "Jane" });

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateReturnsForbidWhenCallerIsNotOwner()
        {
            // Arrange — controller authenticated as userA, but request targets userB
            var userA = Guid.NewGuid();
            var userB = Guid.NewGuid();
            _controller.ControllerContext = MakeContext(userA);

            // Act
            var result = await _controller.Update(userB, new UpdateUserRequest { Name = "Hacker" });

            // Assert
            Assert.IsType<ForbidResult>(result);
            _mockService.Verify(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateUserRequest>()), Times.Never);
        }

        [Fact]
        public async Task UpdateReturnsForbidWhenNoClaimsPresent()
        {
            // Arrange — controller has no authenticated user
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            // Act
            var result = await _controller.Update(Guid.NewGuid(), new UpdateUserRequest { Name = "Ghost" });

            // Assert
            Assert.IsType<ForbidResult>(result);
            _mockService.Verify(s => s.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateUserRequest>()), Times.Never);
        }

        [Fact]
        public async Task UpdateReturnsBadRequest_WhenEmailConflict()
        {
            // Arrange
            var id = Guid.NewGuid();
            _controller.ControllerContext = MakeContext(id);
            var request = new UpdateUserRequest { Name = "User", Email = "taken@example.com" };
            _mockService.Setup(s => s.UpdateAsync(id, request)).ThrowsAsync(new ArgumentException("Email is already registered"));

            // Act
            var result = await _controller.Update(id, request);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        }

        [Fact]
        public async Task DeleteReturnsNoContent_WhenOwner()
        {
            // Arrange
            var id = Guid.NewGuid();
            _controller.ControllerContext = MakeContext(id);
            _mockService.Setup(s => s.DeleteAsync(id)).ReturnsAsync(true);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockService.Verify(s => s.DeleteAsync(id), Times.Once);
        }

        [Fact]
        public async Task DeleteReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var id = Guid.NewGuid();
            _controller.ControllerContext = MakeContext(id);
            _mockService.Setup(s => s.DeleteAsync(id)).ReturnsAsync(false);

            // Act
            var result = await _controller.Delete(id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteReturnsForbid_WhenCallerIsNotOwner()
        {
            // Arrange
            var userA = Guid.NewGuid();
            var userB = Guid.NewGuid();
            _controller.ControllerContext = MakeContext(userA);

            // Act
            var result = await _controller.Delete(userB);

            // Assert
            Assert.IsType<ForbidResult>(result);
            _mockService.Verify(s => s.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }
    }
}
