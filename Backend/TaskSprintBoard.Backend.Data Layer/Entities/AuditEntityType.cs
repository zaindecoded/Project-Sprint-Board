namespace TaskSprintBoard.Backend.Data.Entities;

public class AuditEntityType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<AuditLog> Logs { get; set; } = [];
}
