namespace TaskSprintBoard.Backend.Data.Entities;

public class Sprint : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Goal { get; set; } = string.Empty;
    public string Status { get; set; } = "Planned";

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
