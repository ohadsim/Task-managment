using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TaskManagement.Api.Middleware;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Data;
using TaskManagement.Infrastructure.Strategies;

var builder = WebApplication.CreateBuilder(args);

// Resolve the Npgsql connection string.
//
// Priority order:
//   1. DATABASE_URL env var  — Railway injects this from the PostgreSQL service when you
//      add a reference variable like ${{Postgres.DATABASE_URL}} in the backend service.
//      The value is a postgres:// URI, which we parse into a key=value Npgsql string.
//   2. ConnectionStrings__DefaultConnection — standard ASP.NET Core config key, used for
//      local development via appsettings.Development.json.
//
// Why not use ${{Postgres.PGHOST}} etc. individually?
// Those reference variables require knowing the exact service card name in Railway (which
// may be "Postgres", "postgresql", "db", etc.). DATABASE_URL is a single variable that
// avoids the guessing game, and Railway always emits it from the PostgreSQL plugin.
static string? BuildConnectionStringFromDatabaseUrl(string databaseUrl)
{
    // DATABASE_URL format: postgres://user:password@host:port/database
    // or:                  postgresql://user:password@host:port/database
    if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri))
        return null;

    var userInfo = uri.UserInfo.Split(':', 2);
    var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty;
    var password  = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;

    var csb = new NpgsqlConnectionStringBuilder
    {
        Host     = uri.Host,
        Port     = uri.Port > 0 ? uri.Port : 5432,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = username,
        Password = password,
        // Railway's managed Postgres requires SSL; use Prefer so local URIs still work.
        SslMode  = SslMode.Prefer,
    };
    return csb.ConnectionString;
}

// Database — conditionally register provider (InMemory for testing, Npgsql for everything else)
var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");

// Determine the connection string once here so we can log its source at startup.
string? connectionString = null;
if (!useInMemory)
{
    // Try both sources — Environment.GetEnvironmentVariable and builder.Configuration
    // (Railway Dockerfile services can surface vars through either path)
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
                      ?? builder.Configuration["DATABASE_URL"];

    // Diagnostic dump — visible in Railway deploy logs, remove once stable
    Console.WriteLine($"[STARTUP] DATABASE_URL present: {!string.IsNullOrEmpty(databaseUrl)}");
    Console.WriteLine($"[STARTUP] ConnectionStrings:DefaultConnection present: {!string.IsNullOrEmpty(builder.Configuration.GetConnectionString("DefaultConnection"))}");
    Console.WriteLine($"[STARTUP] ASPNETCORE_ENVIRONMENT: {builder.Configuration["ASPNETCORE_ENVIRONMENT"] ?? "not set"}");

    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        connectionString = BuildConnectionStringFromDatabaseUrl(databaseUrl);
        if (connectionString is null)
        {
            Console.Error.WriteLine(
                $"[STARTUP] DATABASE_URL was set but could not be parsed as a postgres:// URI. " +
                $"Value (first 40 chars): {databaseUrl[..Math.Min(40, databaseUrl.Length)]}...");
        }
        else
        {
            Console.WriteLine("[STARTUP] Using connection string built from DATABASE_URL.");
        }
    }

    // Fall back to appsettings / env var ConnectionStrings__DefaultConnection
    connectionString ??= builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Console.Error.WriteLine(
            "[STARTUP] No database connection string found. " +
            "Set DATABASE_URL or ConnectionStrings__DefaultConnection in Railway Variables.");
    }
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (useInMemory)
        options.UseInMemoryDatabase("TaskManagement");
    else
        options.UseNpgsql(connectionString);
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
    try
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
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database migration/seed failed on startup. Verify ConnectionStrings__DefaultConnection is set correctly.");
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
