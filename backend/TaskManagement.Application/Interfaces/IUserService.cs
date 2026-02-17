using TaskManagement.Application.DTOs;

namespace TaskManagement.Application.Interfaces;

public interface IUserService
{
    Task<List<TaskResponse>> GetUserTasksAsync(int userId);
    Task<List<UserResponse>> GetAllUsersAsync();
}
