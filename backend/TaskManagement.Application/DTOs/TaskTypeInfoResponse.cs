using TaskManagement.Application.Interfaces;

namespace TaskManagement.Application.DTOs;

/// <summary>
/// Returned by GET /api/task-types so the frontend knows what types exist
/// and their status definitions + field requirements.
/// </summary>
public class TaskTypeInfoResponse
{
    public string TaskType { get; set; } = string.Empty;
    public int MaxStatus { get; set; }
    public List<StatusDefinition> Statuses { get; set; } = new();
    public Dictionary<int, List<FieldDefinition>> FieldsByStatus { get; set; } = new();
}
