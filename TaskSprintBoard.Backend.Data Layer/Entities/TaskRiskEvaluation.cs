namespace TaskSprintBoard.Backend.Data.Entities;

public class TaskRiskEvaluation : BaseEntity
{
    public int TaskId { get; set; }
    public TaskItem Task { get; set; } = null!;

    public decimal EstimateHours { get; set; }
    public int TaskAge { get; set; }
    public int DaysRemaining { get; set; }
    public int ProgressPercentage { get; set; }
    public bool IsBlocked { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;
    public int NumberOfStatusChanges { get; set; }
    public decimal RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string RiskFactors { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
    public string? EvaluatedBy { get; set; }
}
