using TaskManagement.Application.DTOs;

namespace TaskManagement.Application.Interfaces;

public interface ITaskTypeService
{
    List<TaskTypeInfoResponse> GetAllTaskTypes();
}
