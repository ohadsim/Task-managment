using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// GET /api/users - Get all users.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> GetAll()
    {
        var result = await _userService.GetAllUsersAsync();
        return Ok(result);
    }

    /// <summary>
    /// GET /api/users/{id}/tasks - Get all tasks assigned to a user.
    /// </summary>
    [HttpGet("{id}/tasks")]
    public async Task<ActionResult<List<TaskResponse>>> GetUserTasks(int id)
    {
        var result = await _userService.GetUserTasksAsync(id);
        return Ok(result);
    }
}
