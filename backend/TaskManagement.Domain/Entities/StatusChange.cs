namespace TaskManagement.Domain.Entities;

public class StatusChange
{
    public int Id { get; set; }
    public int TaskItemId { get; set; }
    public TaskItem TaskItem { get; set; } = null!;
    public int FromStatus { get; set; }
    public int ToStatus { get; set; }
    public int AssignedUserId { get; set; }
    public User AssignedUser { get; set; } = null!;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
