namespace TaskSprintBoard.Backend.Data.Entities;

public class Comment : BaseEntity
{
    public int TaskId { get; set; }
    public TaskItem Task { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Message { get; set; } = string.Empty;
}
