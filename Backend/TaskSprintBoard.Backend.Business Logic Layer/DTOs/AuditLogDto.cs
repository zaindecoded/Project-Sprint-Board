namespace TaskSprintBoard.Backend.BusinessLogic.DTOs;

public class AuditLogDto
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public int? ActorUserId { get; set; }
    public string Actor { get; set; } = "Unknown user";
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? Details { get; set; }
}

public class AuditLogPageDto
{
    public IReadOnlyList<AuditLogDto> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
