using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Middleware;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Data;
using TaskManagement.Infrastructure.Strategies;

var builder = WebApplication.CreateBuilder(args);

// Database — conditionally register provider (InMemory for testing, Npgsql for everything else)
var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (useInMemory)
        options.UseInMemoryDatabase("TaskManagement");
    else
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Register AppDbContext as DbContext so TaskService can receive it
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());

// Strategy registrations -- THIS IS WHERE NEW TASK TYPES ARE ADDED
builder.Services.AddScoped<ITaskTypeStrategy, ProcurementTaskStrategy>();
builder.Services.AddScoped<ITaskTypeStrategy, DevelopmentTaskStrategy>();

// Services
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITaskTypeService, TaskTypeService>();

// Controllers + JSON
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });

// CORS — read allowed origins from config, fall back to localhost for dev
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];
if (allowedOrigins.Length == 0)
    allowedOrigins = ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();

// Minimal health endpoint — required by railway.toml healthcheckPath
// Returns 200 OK so Railway knows the container is ready to serve traffic.
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Auto-migrate and seed when using a real database (safe: MigrateAsync is idempotent)
if (!useInMemory)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    // Seed 6 demo tasks if the table is empty
    if (!db.Tasks.Any())
    {
        db.Tasks.AddRange(
            new TaskItem { Title = "Purchase office laptops", TaskType = "Procurement", AssignedUserId = 1, CurrentStatus = 1 },
            new TaskItem { Title = "Purchase monitors", TaskType = "Procurement", AssignedUserId = 2, CurrentStatus = 1 },
            new TaskItem { Title = "Purchase office furniture", TaskType = "Procurement", AssignedUserId = 3, CurrentStatus = 1 },
            new TaskItem { Title = "Build REST API", TaskType = "Development", AssignedUserId = 1, CurrentStatus = 1 },
            new TaskItem { Title = "Implement authentication", TaskType = "Development", AssignedUserId = 2, CurrentStatus = 1 },
            new TaskItem { Title = "Build dashboard", TaskType = "Development", AssignedUserId = 4, CurrentStatus = 1 }
        );
        await db.SaveChangesAsync();
    }
}

// Railway sets PORT env var — bind to it when present so the platform can route traffic
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    app.Urls.Clear();
    app.Urls.Add($"http://0.0.0.0:{port}");
}

app.Run();

// Make Program class accessible for WebApplicationFactory in tests
public partial class Program { }
