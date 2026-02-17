using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<StatusChange> StatusChanges => Set<StatusChange>();
    public DbSet<User> Users => Set<User>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Seed users
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Name = "Alice Johnson", Email = "alice@example.com" },
            new User { Id = 2, Name = "Bob Smith", Email = "bob@example.com" },
            new User { Id = 3, Name = "Charlie Brown", Email = "charlie@example.com" },
            new User { Id = 4, Name = "Diana Prince", Email = "diana@example.com" }
        );
    }
}
