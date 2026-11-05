namespace TaskSprintBoard.Backend.BusinessLogic.DTOs;

public class SprintCreateDto
{
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Goal { get; set; } = string.Empty;
    public int StatusId { get; set; } = 1;
}

public class SprintUpdateDto
{
    public int? ProjectId { get; set; }
    public string? Name { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Goal { get; set; }
    public int? StatusId { get; set; }
}

public class SprintDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Goal { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
