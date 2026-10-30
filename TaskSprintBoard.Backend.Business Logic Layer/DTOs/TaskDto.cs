using System.ComponentModel.DataAnnotations;

namespace TaskSprintBoard.Backend.BusinessLogic.DTOs;

public class TaskCreateDto
{
    public int SprintId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Todo";
    public decimal EstimateHours { get; set; }
    public int ProgressPercentage { get; set; }
    public DateTime? StartDate { get; set; }
    [Required]
    public DateTime? DueDate { get; set; }
    public string? BlockerInformation { get; set; }
}

public class TaskUpdateDto
{
    public int? SprintId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public decimal? EstimateHours { get; set; }
    public int? ProgressPercentage { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? BlockerInformation { get; set; }
}

public class TaskDto
{
    public int Id { get; set; }
    public int SprintId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal EstimateHours { get; set; }
    public int ProgressPercentage { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? BlockerInformation { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
