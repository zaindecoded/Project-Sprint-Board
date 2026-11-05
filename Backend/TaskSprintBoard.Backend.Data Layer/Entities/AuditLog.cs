namespace TaskSprintBoard.Backend.Data.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public int? ActorUserId { get; set; }
    public string? ActorName { get; set; }
    public int ActionId { get; set; }
    public AuditAction ActionDefinition { get; set; } = null!;
    public int EntityTypeId { get; set; }
    public AuditEntityType EntityTypeDefinition { get; set; } = null!;
    public int? EntityId { get; set; }
    public string? Details { get; set; }
}
