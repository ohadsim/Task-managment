using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// POST /api/tasks - Create a new task.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TaskResponse>> Create(CreateTaskRequest request)
    {
        var result = await _taskService.CreateTaskAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// GET /api/tasks/{id} - Get a task by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TaskResponse>> GetById(int id)
    {
        var result = await _taskService.GetTaskByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// PUT /api/tasks/{id}/status - Change the status of a task.
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<ActionResult<TaskResponse>> ChangeStatus(int id, ChangeStatusRequest request)
    {
        var result = await _taskService.ChangeStatusAsync(id, request);
        return Ok(result);
    }

    /// <summary>
    /// PUT /api/tasks/{id}/close - Close a task.
    /// </summary>
    [HttpPut("{id}/close")]
    public async Task<ActionResult<TaskResponse>> Close(int id)
    {
        var result = await _taskService.CloseTaskAsync(id);
        return Ok(result);
    }
}
