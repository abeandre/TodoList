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

public class ToDoControllerIntegrationTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Guid _testUserId = Guid.NewGuid();

    private const string TestJwtKey = "IntegrationTestKey-MustBe32CharsOrMore!!";

    public ToDoControllerIntegrationTests()
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
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GenerateToken());
    }

    private string GenerateToken()
    {
        var key = Encoding.UTF8.GetBytes(TestJwtKey);
        var handler = new JwtSecurityTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Sub, _testUserId.ToString()) }),
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
    public async Task Create_ReturnsCreated_WithAllMappedFields()
    {
        var response = await _client.PostAsJsonAsync("/api/todo",
            new CreateToDoRequest { Title = "Integration Test", Description = "Desc" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var todo = await response.Content.ReadFromJsonAsync<ToDoResponse>();
        Assert.NotNull(todo);
        Assert.NotEqual(Guid.Empty, todo.Id);
        Assert.Equal("Integration Test", todo.Title);
        Assert.Equal("Desc", todo.Description);
        Assert.NotEqual(default, todo.CreatedAt);
        Assert.NotEqual(default, todo.UpdatedAt);
        Assert.Null(todo.FinishedAt);
    }

    [Fact]
    public async Task GetAll_ReturnsPreviouslyCreatedTodo()
    {
        await _client.PostAsJsonAsync("/api/todo",
            new CreateToDoRequest { Title = "GetAll Test" });

        var response = await _client.GetAsync("/api/todo");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var todos = await response.Content.ReadFromJsonAsync<List<ToDoResponse>>();
        Assert.NotNull(todos);
        Assert.Contains(todos, t => t.Title == "GetAll Test");
    }

    [Fact]
    public async Task Create_WithEmptyTitle_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/todo",
            new CreateToDoRequest { Title = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithTitleAtMaxLength_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/todo",
            new CreateToDoRequest { Title = new string('a', 200) });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithTitleExceedingMaxLength_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/todo",
            new CreateToDoRequest { Title = new string('a', 201) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithDescriptionAtMaxLength_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/todo",
            new CreateToDoRequest { Title = "Test", Description = new string('a', 2000) });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithDescriptionExceedingMaxLength_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/todo",
            new CreateToDoRequest { Title = "Test", Description = new string('a', 2001) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithXssInTitle_StoredTitleDoesNotContainRawTags()
    {
        var response = await _client.PostAsJsonAsync("/api/todo",
            new CreateToDoRequest { Title = "<script>alert('xss')</script>" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var todo = await response.Content.ReadFromJsonAsync<ToDoResponse>();
        Assert.NotNull(todo);
        Assert.DoesNotContain("<script>", todo.Title);
        Assert.DoesNotContain("</script>", todo.Title);
    }

    [Fact]
    public async Task Create_WithXssInDescription_StoredDescriptionDoesNotContainRawTags()
    {
        var response = await _client.PostAsJsonAsync("/api/todo",
            new CreateToDoRequest
            {
                Title = "XSS Desc Test",
                Description = "<img src=x onerror=alert(1)>"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var todo = await response.Content.ReadFromJsonAsync<ToDoResponse>();
        Assert.NotNull(todo);
        Assert.DoesNotContain("<img", todo.Description);
    }

    [Fact]
    public async Task Update_WithXssInTitle_StoredTitleDoesNotContainRawTags()
    {
        var create = await _client.PostAsJsonAsync("/api/todo",
            new CreateToDoRequest { Title = "Original" });
        var created = await create.Content.ReadFromJsonAsync<ToDoResponse>();

        await _client.PutAsJsonAsync($"/api/todo/{created!.Id}",
            new UpdateToDoRequest { Title = "<script>xss</script>" });

        var get = await _client.GetAsync("/api/todo");
        var all = await get.Content.ReadFromJsonAsync<List<ToDoResponse>>();
        var updated = all?.Find(t => t.Id == created.Id);
        Assert.NotNull(updated);
        Assert.DoesNotContain("<script>", updated.Title);
    }

    [Fact]
    public async Task Update_WithEmptyTitle_ReturnsBadRequest()
    {
        var create = await _client.PostAsJsonAsync("/api/todo",
            new CreateToDoRequest { Title = "ToUpdate" });
        var created = await create.Content.ReadFromJsonAsync<ToDoResponse>();

        var response = await _client.PutAsJsonAsync($"/api/todo/{created!.Id}",
            new UpdateToDoRequest { Title = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithOversizedTitle_ReturnsBadRequest()
    {
        var create = await _client.PostAsJsonAsync("/api/todo",
            new CreateToDoRequest { Title = "ToUpdate" });
        var created = await create.Content.ReadFromJsonAsync<ToDoResponse>();

        var response = await _client.PutAsJsonAsync($"/api/todo/{created!.Id}",
            new UpdateToDoRequest { Title = new string('a', 201) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
