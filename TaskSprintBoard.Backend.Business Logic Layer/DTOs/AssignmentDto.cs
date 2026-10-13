namespace TaskSprintBoard.Backend.BusinessLogic.DTOs;

public class AssignmentCreateDto
{
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    public DateTime? AssignedAt { get; set; }
    public string? RoleInTask { get; set; }
}

public class AssignmentUpdateDto
{
    public Guid? TaskId { get; set; }
    public Guid? UserId { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? UnassignedAt { get; set; }
    public string? RoleInTask { get; set; }
    public bool? IsActive { get; set; }
}

public class AssignmentResponseDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public DateTime? UnassignedAt { get; set; }
    public string? RoleInTask { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AssignmentDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    public string? RoleInTask { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? UnassignedAt { get; set; }
    public string UserName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
