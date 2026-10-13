namespace TaskSprintBoard.Backend.BusinessLogic.DTOs;

public class SprintCreateDto
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Goal { get; set; } = string.Empty;
    public string Status { get; set; } = "Planned";
}

public class SprintUpdateDto
{
    public Guid? ProjectId { get; set; }
    public string? Name { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Goal { get; set; }
    public string? Status { get; set; }
}

public class SprintDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Goal { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
