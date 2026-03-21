using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ToDo.DataAccess.Repositories;
using ToDoApi.Models;
using ToDoApi.Services;
using Xunit;

namespace ToDoApi.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _mockRepo;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly AuthService _service;

        public AuthServiceTests()
        {
            _mockRepo = new Mock<IUserRepository>();
            _mockConfig = new Mock<IConfiguration>();
            
            _mockConfig.Setup(c => c["Jwt:Key"]).Returns("TestSecretKeyThatIsAtLeast16Or32BytesLongForHmacSha256!!!!!");
            _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            _mockConfig.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

            _service = new AuthService(_mockRepo.Object, _mockConfig.Object, NullLogger<AuthService>.Instance);
        }

        [Fact]
        public async Task AuthenticateAsync_ReturnsNull_WhenUserNotFound()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetUserByEmailAsync("nonexistent@example.com")).ReturnsAsync((ToDo.DataAccess.User?)null);

            // Act
            var result = await _service.AuthenticateAsync(new AuthRequest { Email = "nonexistent@example.com", Password = "pw" });

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AuthenticateAsync_ReturnsNull_WhenPasswordInvalid()
        {
            // Arrange
            var saltBytes = new byte[32];
            RandomNumberGenerator.Fill(saltBytes);
            var salt = Convert.ToBase64String(saltBytes);
            var correctHash = Convert.ToBase64String(
                Rfc2898DeriveBytes.Pbkdf2("correctpw", saltBytes, 100_000, HashAlgorithmName.SHA512, 64));

            var user = new ToDo.DataAccess.User { Id = Guid.NewGuid(), Email = "test@example.com", Salt = salt, HashedPassword = correctHash };
            _mockRepo.Setup(r => r.GetUserByEmailAsync("test@example.com")).ReturnsAsync(user);

            // Act
            var result = await _service.AuthenticateAsync(new AuthRequest { Email = "test@example.com", Password = "wrongpw" });

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AuthenticateAsync_ReturnsAuthResponseAndJwtToken_WhenCredentialsValid()
        {
            // Arrange
            var saltBytes = new byte[32];
            RandomNumberGenerator.Fill(saltBytes);
            var salt = Convert.ToBase64String(saltBytes);
            var correctHash = Convert.ToBase64String(
                Rfc2898DeriveBytes.Pbkdf2("correctpw", saltBytes, 100_000, HashAlgorithmName.SHA512, 64));

            var user = new ToDo.DataAccess.User { Id = Guid.NewGuid(), Name = "Valid User", Email = "test@example.com", Salt = salt, HashedPassword = correctHash };
            _mockRepo.Setup(r => r.GetUserByEmailAsync("test@example.com")).ReturnsAsync(user);

            // Act
            var result = await _service.AuthenticateAsync(new AuthRequest { Email = "test@example.com", Password = "correctpw" });

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Valid User", result!.UserName);
            Assert.Equal("test@example.com", result.Email);
            Assert.False(string.IsNullOrEmpty(result.Token));
            
            // Basic JWT check (3 parts)
            var tokenParts = result.Token.Split('.');
            Assert.Equal(3, tokenParts.Length);
        }
    }
}
