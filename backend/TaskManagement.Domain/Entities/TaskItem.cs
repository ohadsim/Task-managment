namespace TaskManagement.Domain.Entities;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public int CurrentStatus { get; set; } = 1;
    public bool IsClosed { get; set; }
    public int AssignedUserId { get; set; }
    public User AssignedUser { get; set; } = null!;
    public Dictionary<string, object> CustomData { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<StatusChange> StatusHistory { get; set; } = new List<StatusChange>();
}
