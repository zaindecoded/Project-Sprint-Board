namespace TaskSprintBoard.Backend.Data.Entities;

public class Comment : BaseEntity
{
    public Guid TaskId { get; set; }
    public TaskItem Task { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Message { get; set; } = string.Empty;
}
