namespace TaskSprintBoard.Backend.Data.Entities;

public class TaskItem : BaseEntity
{
    public int SprintId { get; set; }
    public Sprint Sprint { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Todo";
    public decimal EstimateHours { get; set; }
    public int ProgressPercentage { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? BlockerInformation { get; set; }

    public ICollection<Assignment> Assignments { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<StatusHistory> StatusHistory { get; set; } = [];
    public ICollection<TaskRiskEvaluation> RiskEvaluations { get; set; } = [];
}
