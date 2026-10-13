namespace TaskSprintBoard.Backend.BusinessLogic.DTOs;

public class AiRiskEvaluationInputDto
{
    public Guid TaskId { get; set; }
    public decimal EstimateHours { get; set; }
    public int TaskAge { get; set; }
    public int DaysRemaining { get; set; }
    public int ProgressPercentage { get; set; }
    public bool IsBlocked { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;
    public int NumberOfStatusChanges { get; set; }
}

public class AiRiskEvaluationResultDto
{
    public Guid TaskId { get; set; }
    public decimal RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string RiskFactors { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
