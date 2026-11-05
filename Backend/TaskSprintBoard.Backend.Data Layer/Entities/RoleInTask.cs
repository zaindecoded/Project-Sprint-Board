namespace TaskSprintBoard.Backend.Data.Entities;

public class RoleInTask
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Assignment> Assignments { get; set; } = [];
}
