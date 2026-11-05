namespace TaskSprintBoard.Backend.Data.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int StatusId { get; set; } = 2;
    public Status StatusDefinition { get; set; } = null!;
    public string Status => StatusDefinition?.Id == StatusId && !string.IsNullOrEmpty(StatusDefinition.Name)
        ? StatusDefinition.Name
        : StatusId switch { 1 => "Planned", 2 => "Active", 3 => "OnHold", 4 => "Completed", 5 => "Cancelled", _ => string.Empty };

    public ICollection<Sprint> Sprints { get; set; } = [];
}
