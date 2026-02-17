using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Errors;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Services;

public class UserService : IUserService
{
    private readonly DbContext _db;
    private readonly IReadOnlyDictionary<string, ITaskTypeStrategy> _strategies;

    public UserService(DbContext db, IEnumerable<ITaskTypeStrategy> strategies)
    {
        _db = db;
        _strategies = strategies.ToDictionary(
            s => s.TaskType,
            s => s,
            StringComparer.OrdinalIgnoreCase
        );
    }

    public async Task<List<TaskResponse>> GetUserTasksAsync(int userId)
    {
        var userExists = await _db.Set<User>().AnyAsync(u => u.Id == userId);
        if (!userExists)
            throw new NotFoundException($"User with ID {userId} not found.");

        var tasks = await _db.Set<TaskItem>()
            .Include(t => t.AssignedUser)
            .Include(t => t.StatusHistory.OrderBy(sh => sh.ChangedAt))
                .ThenInclude(sh => sh.AssignedUser)
            .Where(t => t.AssignedUserId == userId)
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync();

        return tasks.Select(t =>
        {
            var strategy = ResolveStrategy(t.TaskType);
            return TaskMapper.ToResponse(t, strategy);
        }).ToList();
    }

    public async Task<List<UserResponse>> GetAllUsersAsync()
    {
        var users = await _db.Set<User>()
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .ToListAsync();

        return users.Select(TaskMapper.ToResponse).ToList();
    }

    private ITaskTypeStrategy ResolveStrategy(string taskType)
    {
        if (!_strategies.TryGetValue(taskType, out var strategy))
            throw new ValidationException($"Unknown task type: '{taskType}'.");
        return strategy;
    }
}
