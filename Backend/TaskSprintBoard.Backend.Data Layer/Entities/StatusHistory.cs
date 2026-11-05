namespace TaskSprintBoard.Backend.Data.Entities;

public class StatusHistory : BaseEntity
{
    public int TaskId { get; set; }
    public TaskItem Task { get; set; } = null!;

    public int OldStatusId { get; set; }
    public TaskStatusLookup OldStatusDefinition { get; set; } = null!;
    public int NewStatusId { get; set; }
    public TaskStatusLookup NewStatusDefinition { get; set; } = null!;
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
}
