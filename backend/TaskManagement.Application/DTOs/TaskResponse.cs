namespace TaskManagement.Application.DTOs;

public class TaskResponse
{
    public int Id { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int CurrentStatus { get; set; }
    public string CurrentStatusLabel { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
    public int AssignedUserId { get; set; }
    public string AssignedUserName { get; set; } = string.Empty;
    public Dictionary<string, object> CustomData { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<StatusHistoryResponse> StatusHistory { get; set; } = new();
}
