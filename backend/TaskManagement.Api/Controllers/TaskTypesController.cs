using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Route("api/task-types")]
public class TaskTypesController : ControllerBase
{
    private readonly ITaskTypeService _taskTypeService;

    public TaskTypesController(ITaskTypeService taskTypeService)
    {
        _taskTypeService = taskTypeService;
    }

    /// <summary>
    /// GET /api/task-types - Get all registered task type configurations.
    /// </summary>
    [HttpGet]
    public ActionResult<List<TaskTypeInfoResponse>> GetAll()
    {
        var result = _taskTypeService.GetAllTaskTypes();
        return Ok(result);
    }
}
