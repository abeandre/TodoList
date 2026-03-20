using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using ToDo.DataAccess;
using ToDo.DataAccess.Repositories;
using ToDoApi.Filters;
using ToDoApi.Mappings;
using ToDoApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
    options.Filters.Add<SanitizeStringsFilter>());

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase(builder.Configuration.GetValue<string>("InMemoryDbName") ?? "TodoList")
        .ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));

builder.Services.AddScoped<IToDoRepository, ToDoRepository>();
builder.Services.AddScoped<IToDoService, ToDoService>();
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<ToDoMappingProfile>());

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

// explicit CORS policy; populate Cors:AllowedOrigins in appsettings per environment
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy("FrontendPolicy", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

var app = builder.Build();

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

// HTTPS redirect only outside development to avoid breaking HTTP-only dev/test flows
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors("FrontendPolicy");
app.MapControllers();

app.Run();

public partial class Program { }
