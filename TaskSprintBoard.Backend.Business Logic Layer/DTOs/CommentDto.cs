namespace TaskSprintBoard.Backend.BusinessLogic.DTOs;

public class CommentCreateDto
{
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CommentUpdateDto
{
    public int? TaskId { get; set; }
    public int? UserId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CommentResponseDto
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}