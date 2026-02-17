namespace TaskManagement.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
}
