namespace TaskSprintBoard.Backend.BusinessLogic.DTOs;

public class AssignmentCreateDto
{
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public DateTime? AssignedAt { get; set; }
    public int RoleInTaskId { get; set; } = 3;
}

public class AssignmentUpdateDto
{
    public int? TaskId { get; set; }
    public int? UserId { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? UnassignedAt { get; set; }
    public int? RoleInTaskId { get; set; }
    public bool? IsActive { get; set; }
}

public class AssignmentResponseDto
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public DateTime? UnassignedAt { get; set; }
    public int RoleInTaskId { get; set; }
    public string RoleInTask { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
