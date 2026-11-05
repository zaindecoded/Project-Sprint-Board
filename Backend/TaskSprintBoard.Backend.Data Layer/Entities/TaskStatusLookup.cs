namespace TaskSprintBoard.Backend.Data.Entities;

public class TaskStatusLookup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<TaskItem> Tasks { get; set; } = [];
}
