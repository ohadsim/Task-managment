namespace TaskManagement.Application.DTOs;

public class ChangeStatusRequest
{
    public int TargetStatus { get; set; }
    public int AssignedUserId { get; set; }
    public Dictionary<string, object> CustomData { get; set; } = new();
}
