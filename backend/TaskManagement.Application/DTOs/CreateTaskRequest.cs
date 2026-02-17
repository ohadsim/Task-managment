namespace TaskManagement.Application.DTOs;

public class CreateTaskRequest
{
    public string TaskType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int AssignedUserId { get; set; }
}
