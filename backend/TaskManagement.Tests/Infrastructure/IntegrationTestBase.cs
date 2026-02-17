using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Tests.Infrastructure;

public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    protected readonly HttpClient Client;
    protected readonly CustomWebApplicationFactory Factory;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Seed database once per test class
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.EnsureCreatedAsync();

        // Seed users if not already present
        if (!db.Users.Any())
        {
            db.Users.AddRange(
                new User { Id = 1, Name = "Alice Johnson", Email = "alice@example.com" },
                new User { Id = 2, Name = "Bob Smith", Email = "bob@example.com" },
                new User { Id = 3, Name = "Charlie Brown", Email = "charlie@example.com" },
                new User { Id = 4, Name = "Diana Prince", Email = "diana@example.com" }
            );
            await db.SaveChangesAsync();
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
