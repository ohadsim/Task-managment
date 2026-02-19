using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<TaskItem> Tasks { get; }
    DbSet<User> Users { get; }
    DbSet<StatusChange> StatusChanges { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
