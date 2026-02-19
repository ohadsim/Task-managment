using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Application.Services;

public class TaskTypeService : ITaskTypeService
{
    private readonly IReadOnlyDictionary<string, ITaskTypeStrategy> _strategies;

    public TaskTypeService(IEnumerable<ITaskTypeStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(
            s => s.TaskType,
            s => s,
            StringComparer.OrdinalIgnoreCase
        );
    }

    public List<TaskTypeInfoResponse> GetAllTaskTypes()
    {
        return _strategies.Values.Select(s => new TaskTypeInfoResponse
        {
            TaskType = s.TaskType,
            MaxStatus = s.MaxStatus,
            Statuses = s.GetStatusDefinitions().ToList(),
            FieldsByStatus = Enumerable.Range(1, s.MaxStatus)
                .Where(status => s.GetRequiredFields(status).Count > 0)
                .ToDictionary(
                    status => status,
                    status => s.GetRequiredFields(status).ToList()
                )
        }).ToList();
    }
}