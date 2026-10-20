namespace TaskSprintBoard.Backend.Data.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = "Active";

    public ICollection<Sprint> Sprints { get; set; } = [];
}
