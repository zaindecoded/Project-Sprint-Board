namespace TaskSprintBoard.Backend.Data.Entities;

public class Sprint : BaseEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Goal { get; set; } = string.Empty;
    public int StatusId { get; set; } = 1;
    public Status StatusDefinition { get; set; } = null!;
    public string Status => StatusDefinition?.Id == StatusId && !string.IsNullOrEmpty(StatusDefinition.Name)
        ? StatusDefinition.Name
        : StatusId switch { 1 => "Planned", 2 => "Active", 3 => "OnHold", 4 => "Completed", 5 => "Cancelled", _ => string.Empty };

    public ICollection<TaskItem> Tasks { get; set; } = [];
}
