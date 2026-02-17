using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Application.Services;

/// <summary>
/// Dedicated mapper for converting domain entities to API response DTOs.
/// </summary>
public static class TaskMapper
{
    public static TaskResponse ToResponse(TaskItem task, ITaskTypeStrategy strategy)
    {
        var statusDefs = strategy.GetStatusDefinitions();
        var currentLabel = statusDefs.FirstOrDefault(s => s.Status == task.CurrentStatus)?.Label ?? "Unknown";

        return new TaskResponse
        {
            Id = task.Id,
            TaskType = task.TaskType,
            Title = task.Title,
            CurrentStatus = task.CurrentStatus,
            CurrentStatusLabel = currentLabel,
            IsClosed = task.IsClosed,
            AssignedUserId = task.AssignedUserId,
            AssignedUserName = task.AssignedUser?.Name ?? string.Empty,
            CustomData = task.CustomData,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            StatusHistory = task.StatusHistory
                .OrderBy(sh => sh.ChangedAt)
                .Select(sh => new StatusHistoryResponse
                {
                    FromStatus = sh.FromStatus,
                    ToStatus = sh.ToStatus,
                    AssignedUserId = sh.AssignedUserId,
                    AssignedUserName = sh.AssignedUser?.Name ?? string.Empty,
                    ChangedAt = sh.ChangedAt
                }).ToList()
        };
    }

    public static UserResponse ToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email
        };
    }
}
