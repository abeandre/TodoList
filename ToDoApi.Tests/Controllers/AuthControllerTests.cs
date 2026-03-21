using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ToDoApi.Controllers;
using ToDoApi.Models;
using ToDoApi.Services;
using Xunit;

namespace ToDoApi.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockService;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockService = new Mock<IAuthService>();
            _controller = new AuthController(_mockService.Object);
        }

        [Fact]
        public async Task Login_ReturnsOk_WithAuthResponse_WhenValid()
        {
            // Arrange
            var request = new AuthRequest { Email = "test@example.com", Password = "pw" };
            var responseData = new AuthResponse { Token = "jwt.token.here", UserName = "Test", Email = "test@example.com" };
            _mockService.Setup(s => s.AuthenticateAsync(request)).ReturnsAsync(responseData);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<AuthResponse>(okResult.Value);
            Assert.Equal("jwt.token.here", returned.Token);
            Assert.Equal("Test", returned.UserName);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenInvalid()
        {
            // Arrange
            var request = new AuthRequest { Email = "test@example.com", Password = "wrongpw" };
            _mockService.Setup(s => s.AuthenticateAsync(request)).ReturnsAsync((AuthResponse?)null);

            // Act
            var result = await _controller.Login(request);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result.Result);
        }
    }
}
