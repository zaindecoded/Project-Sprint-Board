namespace TaskSprintBoard.Backend.Data.Entities;

public class Status
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Project> Projects { get; set; } = [];
    public ICollection<Sprint> Sprints { get; set; } = [];
}
