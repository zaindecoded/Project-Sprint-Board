namespace TaskSprintBoard.Backend.Data.Entities;

public class Assignment : BaseEntity
{
    public int TaskId { get; set; }
    public TaskItem Task { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UnassignedAt { get; set; }
    public string? RoleInTask { get; set; }
}
