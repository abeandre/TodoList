using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using ToDo.DataAccess;
using ToDo.DataAccess.Repositories;
using ToDoApi.Filters;
using ToDoApi.Mappings;
using ToDoApi.Services;

// JWT signing key must be provided via environment variable or user secrets — never in source control.
// Set via env var:       JWT__Key=<32+ char secret>
// Set via user secrets:  dotnet user-secrets set "Jwt:Key" "<32+ char secret>"
var builder = WebApplication.CreateBuilder(args);

// Global request body size cap; override with [RequestSizeLimit] on endpoints that need larger payloads.
builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = 1 * 1024 * 1024); // 1 MB

builder.Services.AddControllers(options =>
    options.Filters.Add<SanitizeStringsFilter>());

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase(builder.Configuration.GetValue<string>("InMemoryDbName") ?? "TodoList")
        .ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));

builder.Services.AddScoped<IToDoRepository, ToDoRepository>();
builder.Services.AddScoped<IToDoService, ToDoService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddAutoMapper(cfg => 
{
    cfg.AddProfile<ToDoMappingProfile>();
    cfg.AddProfile<UserMappingProfile>();
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? string.Empty))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Fall back to httpOnly cookie when no Authorization header is present
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader))
                    context.Token = context.Request.Cookies["jwt"];
                return Task.CompletedTask;
            }
        };
    });
    
builder.Services.AddAuthorization();

// Per-IP sliding-window rate limits on sensitive unauthenticated endpoints.
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 2,
                PermitLimit = 10,
                QueueLimit = 0
            }));

    options.AddPolicy("registration", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 2,
                PermitLimit = 5,
                QueueLimit = 0
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// HSTS: instruct browsers to only contact this host over HTTPS for one year.
// Enable Preload only after submitting to the HSTS preload list (https://hstspreload.org).
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

// explicit CORS policy; populate Cors:AllowedOrigins in appsettings per environment.
// In production set via environment variable:  CORS__AllowedOrigins__0=https://app.example.com
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
    options.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

var app = builder.Build();

// Warn loudly at startup when CORS origins are not configured in non-Development environments.
// All cross-origin requests will be rejected until CORS__AllowedOrigins__0 is set.
if (!app.Environment.IsDevelopment() && allowedOrigins.Length == 0)
    app.Logger.LogCritical(
        "Cors:AllowedOrigins is empty. Set at least one allowed origin via the " +
        "CORS__AllowedOrigins__0 environment variable (e.g. https://app.example.com). " +
        "A wildcard origin cannot be used when credentials are enabled.");

// global exception handler; returns RFC 7807 ProblemDetails, hides internals in production
app.UseExceptionHandler(errorApp =>
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        var detail = app.Environment.IsDevelopment()
            ? context.Features.Get<IExceptionHandlerFeature>()?.Error.Message
            : null;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = detail,
        });
    }));

// Scalar/OpenAPI only in development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// HSTS and HTTPS redirect only outside development to avoid breaking HTTP-only dev/test flows
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors("FrontendPolicy");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
