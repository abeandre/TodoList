using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Net.Http.Headers;
using ToDo.DataAccess;
using ToDoApi.Models;

namespace ToDoApi.Tests.Integration;

public class UserControllerIntegrationTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    // A stable test key injected at startup — never used outside tests.
    private const string TestJwtKey = "IntegrationTestKey-MustBe32CharsOrMore!!";

    public UserControllerIntegrationTests()
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

    private string GenerateToken(Guid userId)
    {
        var key = Encoding.UTF8.GetBytes(TestJwtKey);
        var handler = new JwtSecurityTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()) }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "ToDoApi",
            Audience = "ToDoAppClients",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Create_ReturnsCreated_WithValidPayload()
    {
        var response = await _client.PostAsJsonAsync("/api/user",
            new CreateUserRequest { Name = "Int Test User", Email = "int@test.com", Password = "pw" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(user);
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("Int Test User", user.Name);
        Assert.Equal("int@test.com", user.Email);
    }

    [Fact]
    public async Task Create_WithEmptyName_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/user",
            new CreateUserRequest { Name = "", Email = "int@test.com", Password = "pw" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithEmptyEmail_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/user",
            new CreateUserRequest { Name = "John", Email = "", Password = "pw" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithEmailExceedingMaxLength_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/user",
            new CreateUserRequest { Name = "John", Email = new string('a', 201), Password = "pw" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ReturnsNoContent_WhenOwnerUpdatesOwnProfile()
    {
        // Arrange — create user, then authenticate as that user
        var createResponse = await _client.PostAsJsonAsync("/api/user",
            new CreateUserRequest { Name = "ToUpdate", Email = "update@test.com", Password = "pw" });
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(createdUser!.Id));

        // Act
        var updateResponse = await _client.PutAsJsonAsync($"/api/user/{createdUser.Id}",
            new UpdateUserRequest { Name = "Updated Name", Email = "updated@test.com" });

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
    }

    [Fact]
    public async Task Update_ReturnsForbidden_WhenCallerIsNotOwner()
    {
        // Arrange — create user, but authenticate as a different random user
        var createResponse = await _client.PostAsJsonAsync("/api/user",
            new CreateUserRequest { Name = "Victim", Email = "victim@test.com", Password = "pw" });
        var victim = await createResponse.Content.ReadFromJsonAsync<UserResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(Guid.NewGuid()));

        // Act
        var response = await _client.PutAsJsonAsync($"/api/user/{victim!.Id}",
            new UpdateUserRequest { Name = "Hijacked", Email = "hijacked@test.com" });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithEmptyName_ReturnsBadRequest()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/user",
            new CreateUserRequest { Name = "ToUpdate", Email = "update2@test.com", Password = "pw" });
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(createdUser!.Id));

        var response = await _client.PutAsJsonAsync($"/api/user/{createdUser.Id}",
            new UpdateUserRequest { Name = "", Email = "updated@test.com" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var request1 = new CreateUserRequest { Name = "First User", Email = "duplicate@test.com", Password = "pw" };
        var request2 = new CreateUserRequest { Name = "Second User", Email = "duplicate@test.com", Password = "pw" };

        var response1 = await _client.PostAsJsonAsync("/api/user", request1);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

        // Act
        var response2 = await _client.PostAsJsonAsync("/api/user", request2);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
        var content = await response2.Content.ReadAsStringAsync();
        Assert.Contains("Email is already registered", content);
    }
}
