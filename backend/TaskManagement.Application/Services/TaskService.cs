using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Errors;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Services;

public class TaskService : ITaskService
{
    private readonly IAppDbContext _db;
    private readonly IReadOnlyDictionary<string, ITaskTypeStrategy> _strategies;

    public TaskService(IAppDbContext db, IEnumerable<ITaskTypeStrategy> strategies)
    {
        _db = db;
        _strategies = strategies.ToDictionary(
            s => s.TaskType,
            s => s,
            StringComparer.OrdinalIgnoreCase
        );
    }

    public async Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TaskType))
            throw new ValidationException("Task type is required.");

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ValidationException("Title is required.");

        if (request.AssignedUserId <= 0)
            throw new ValidationException("Assigned user is required.");

        var strategy = ResolveStrategy(request.TaskType);

        var user = await _db.Users.FindAsync(request.AssignedUserId);
        if (user is null)
            throw new NotFoundException($"User with ID {request.AssignedUserId} not found.");

        var now = DateTime.UtcNow;
        var task = new TaskItem
        {
            Title = request.Title,
            TaskType = strategy.TaskType,
            CurrentStatus = 1,
            IsClosed = false,
            AssignedUserId = request.AssignedUserId,
            CustomData = new Dictionary<string, object>(),
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        task.AssignedUser = user;
        return TaskMapper.ToResponse(task, strategy);
    }

    public async Task<TaskResponse> GetTaskByIdAsync(int taskId)
    {
        var task = await _db.Tasks
            .Include(t => t.AssignedUser)
            .Include(t => t.StatusHistory)
                .ThenInclude(sh => sh.AssignedUser)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task is null)
            throw new NotFoundException($"Task with ID {taskId} not found.");

        var strategy = ResolveStrategy(task.TaskType);
        return TaskMapper.ToResponse(task, strategy);
    }

    public async Task<TaskResponse> ChangeStatusAsync(int taskId, ChangeStatusRequest request)
    {
        if (request.AssignedUserId <= 0)
            throw new ValidationException("Next assigned user is required.");

        if (request.TargetStatus < 1)
            throw new ValidationException("Target status must be at least 1.");

        var task = await _db.Tasks
            .Include(t => t.AssignedUser)
            .Include(t => t.StatusHistory)
                .ThenInclude(sh => sh.AssignedUser)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task is null)
            throw new NotFoundException($"Task with ID {taskId} not found.");

        if (task.IsClosed)
            throw new ValidationException("Cannot change status of a closed task.");

        var strategy = ResolveStrategy(task.TaskType);

        var targetStatus = request.TargetStatus;
        var currentStatus = task.CurrentStatus;

        if (targetStatus == currentStatus)
            throw new ValidationException($"Task is already at status {currentStatus}.");

        bool isForward = targetStatus > currentStatus;

        if (isForward)
        {
            if (targetStatus != currentStatus + 1)
                throw new ValidationException(
                    $"Forward moves must be sequential. Current status is {currentStatus}, target must be {currentStatus + 1}.");

            if (targetStatus > strategy.MaxStatus)
                throw new ValidationException(
                    $"Target status {targetStatus} exceeds the maximum status {strategy.MaxStatus} for task type '{strategy.TaskType}'.");

            var customData = request.CustomData ?? new Dictionary<string, object>();
            var errors = strategy.ValidateStatusData(targetStatus, customData);
            if (errors.Count > 0)
                throw new ValidationException(errors);

            // Merge custom data into existing (create new dictionary to trigger EF change detection)
            var merged = new Dictionary<string, object>(task.CustomData);
            foreach (var kvp in customData)
            {
                merged[kvp.Key] = kvp.Value;
            }
            task.CustomData = merged;
        }
        else
        {
            if (targetStatus < 1)
                throw new ValidationException("Target status cannot be less than 1.");
        }

        var nextUser = await _db.Users.FindAsync(request.AssignedUserId);
        if (nextUser is null)
            throw new NotFoundException($"User with ID {request.AssignedUserId} not found.");

        var fromStatus = task.CurrentStatus;
        task.CurrentStatus = targetStatus;
        task.AssignedUserId = request.AssignedUserId;
        task.AssignedUser = nextUser;
        task.UpdatedAt = DateTime.UtcNow;

        var statusChange = new StatusChange
        {
            TaskItemId = task.Id,
            FromStatus = fromStatus,
            ToStatus = targetStatus,
            AssignedUserId = request.AssignedUserId,
            ChangedAt = DateTime.UtcNow
        };

        _db.StatusChanges.Add(statusChange);
        await _db.SaveChangesAsync();

        // Reload to get the full navigation properties on the new status change
        statusChange.AssignedUser = nextUser;

        return TaskMapper.ToResponse(task, strategy);
    }

    public async Task<TaskResponse> CloseTaskAsync(int taskId)
    {
        var task = await _db.Tasks
            .Include(t => t.AssignedUser)
            .Include(t => t.StatusHistory)
                .ThenInclude(sh => sh.AssignedUser)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task is null)
            throw new NotFoundException($"Task with ID {taskId} not found.");

        if (task.IsClosed)
            throw new ValidationException("Task is already closed.");

        var strategy = ResolveStrategy(task.TaskType);

        if (task.CurrentStatus != strategy.MaxStatus)
            throw new ValidationException(
                $"Task can only be closed from the final status ({strategy.MaxStatus}). Current status is {task.CurrentStatus}.");

        task.IsClosed = true;
        task.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return TaskMapper.ToResponse(task, strategy);
    }

    private ITaskTypeStrategy ResolveStrategy(string taskType)
    {
        if (!_strategies.TryGetValue(taskType, out var strategy))
            throw new ValidationException($"Unknown task type: '{taskType}'.");
        return strategy;
    }
}
