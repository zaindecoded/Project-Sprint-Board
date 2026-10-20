namespace TaskSprintBoard.Backend.BusinessLogic.DTOs;

public class StatusHistoryResponseDto
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Reason { get; set; }
    public bool IsActive { get; set; }
}
