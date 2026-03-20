using Scalar.AspNetCore;
using ToDo.DataAccess;
using ToDo.DataAccess.Repositories;
using ToDoApi.Filters;
using ToDoApi.Mappings;
using ToDoApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
    options.Filters.Add<SanitizeStringsFilter>());
builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddScoped<IToDoRepository, ToDoRepository>();
builder.Services.AddScoped<IToDoService, ToDoService>();
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<ToDoMappingProfile>());

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
