namespace TaskSprintBoard.Backend.BusinessLogic.DTOs;

public class CommentCreateDto
{
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CommentUpdateDto
{
    public Guid? TaskId { get; set; }
    public Guid? UserId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CommentResponseDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CommentDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
