using TaskManagement.Application.DTOs;

namespace TaskManagement.Application.Interfaces;

public interface ITaskService
{
    Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request);
    Task<TaskResponse> GetTaskByIdAsync(int taskId);
    Task<TaskResponse> ChangeStatusAsync(int taskId, ChangeStatusRequest request);
    Task<TaskResponse> CloseTaskAsync(int taskId);
}
