using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ToDo.DataAccess;
using ToDoApi.Models;

namespace ToDoApi.Tests.Integration;

public class AuthControllerIntegrationTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    private const string TestJwtKey = "IntegrationTestKey-MustBe32CharsOrMore!!";

    public AuthControllerIntegrationTests()
    {
        var dbName = Guid.NewGuid().ToString();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, cfg) =>
                cfg.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = TestJwtKey }));

            builder.ConfigureServices(services =>
            {
                var toRemove = services
                    .Where(d =>
                        d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                        d.ServiceType == typeof(AppDbContext))
                    .ToList();
                foreach (var d in toRemove) services.Remove(d);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase(dbName)
                           .ConfigureWarnings(w =>
                               w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
            });
        });

        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Login_ReturnsOk_WithJwtToken_WhenCredentialsAreValid()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/user",
            new CreateUserRequest { Name = "Auth Setup", Email = "valid@test.com", Password = "correctpassword" });
        createResponse.EnsureSuccessStatusCode();

        // Act
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new AuthRequest { Email = "valid@test.com", Password = "correctpassword" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResult);
        Assert.Equal("Auth Setup", authResult.UserName);
        Assert.Equal("valid@test.com", authResult.Email);
        // JWT is delivered via httpOnly cookie, not the response body
        var setCookies = loginResponse.Headers.GetValues("Set-Cookie");
        Assert.Contains(setCookies, v => v.StartsWith("jwt=") && v.Contains("httponly", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordIsIncorrect()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/user",
            new CreateUserRequest { Name = "Auth Setup", Email = "invalidpw@test.com", Password = "correctpassword" });
        createResponse.EnsureSuccessStatusCode();

        // Act
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new AuthRequest { Email = "invalidpw@test.com", Password = "wrongpassword" });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenEmailDoesNotExist()
    {
        // Act
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new AuthRequest { Email = "notfound@test.com", Password = "password123" });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }
}
