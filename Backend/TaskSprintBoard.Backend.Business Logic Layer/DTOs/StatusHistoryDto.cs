namespace TaskSprintBoard.Backend.BusinessLogic.DTOs;

public class StatusHistoryResponseDto
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int OldStatusId { get; set; }
    public int NewStatusId { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Reason { get; set; }
    public bool IsActive { get; set; }
}
