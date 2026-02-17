namespace TaskManagement.Application.DTOs;

public class StatusHistoryResponse
{
    public int FromStatus { get; set; }
    public int ToStatus { get; set; }
    public int AssignedUserId { get; set; }
    public string AssignedUserName { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}
