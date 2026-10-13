namespace TaskSprintBoard.Backend.BusinessLogic.DTOs;

public class DashboardSummaryDto
{
    public int TotalProjects { get; set; }
    public int CompletedProjects { get; set; }
    public int ActiveProjects { get; set; }
    public int ActiveSprints { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int BlockedTasks { get; set; }
    public int CriticalTasks { get; set; }
    public int HighRiskTasks { get; set; }
}

public class DashboardChartDto
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class DashboardAttentionTaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string? RiskLevel { get; set; }
    public decimal? RiskScore { get; set; }
    public string? RiskFactors { get; set; }
}
