using TaskSprintBoard.Backend.BusinessLogic.DTOs;
using TaskSprintBoard.Backend.Data.Entities;
using TaskSprintBoard.Backend.Data.Repositories;

namespace TaskSprintBoard.Backend.BusinessLogic.Services;

public class AiRiskService
{
    private readonly TaskRiskEvaluationRepository _riskRepository;
    private readonly TaskRepository _taskRepository;
    private readonly StatusHistoryRepository _historyRepository;

    public AiRiskService(
        TaskRiskEvaluationRepository riskRepository,
        TaskRepository taskRepository,
        StatusHistoryRepository historyRepository)
    {
        _riskRepository = riskRepository;
        _taskRepository = taskRepository;
        _historyRepository = historyRepository;
    }

    public async Task<AiRiskEvaluationResultDto> EvaluateAsync(AiRiskEvaluationInputDto dto, int evaluatedBy)
    {
        var task = await _taskRepository.GetByIdAsync(dto.TaskId) ?? throw new KeyNotFoundException("Task not found.");
        if (task.Status is "Done" or "Cancelled")
            throw new InvalidOperationException("Completed or cancelled tasks do not have delivery risk.");
        if (!task.DueDate.HasValue)
            throw new InvalidOperationException("A due date is required to evaluate task risk.");
        if (task.EstimateHours <= 0)
            throw new InvalidOperationException("Estimate hours must be greater than zero.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysRemaining = DateOnly.FromDateTime(task.DueDate.Value).DayNumber - today.DayNumber;
        if (daysRemaining < 0)
            throw new InvalidOperationException("Overdue tasks cannot be evaluated for delivery risk.");

        var input = new AiRiskEvaluationInputDto
        {
            TaskId = task.Id,
            EstimateHours = task.EstimateHours,
            TaskAge = Math.Max(0, (today.DayNumber - DateOnly.FromDateTime(task.CreatedAt).DayNumber)),
            DaysRemaining = daysRemaining,
            ProgressPercentage = task.ProgressPercentage,
            IsBlocked = task.Status == "Blocked",
            PriorityId = task.PriorityId,
            CurrentStatusId = task.StatusId,
            NumberOfStatusChanges = (await _historyRepository.GetByTaskIdAsync(task.Id)).Count()
        };
        var remainingHours = input.EstimateHours * (100 - input.ProgressPercentage) / 100m;

        var score = CalculateRiskScore(input, remainingHours);
        var level = score switch
        {
            >= 70 => "High",
            >= 50 => "Medium",
            _ => "Low"
        };

        var riskFactors = new List<string>();
        if (input.IsBlocked) riskFactors.Add("Task is blocked");
        if (input.ProgressPercentage < 35) riskFactors.Add("Low progress");
        if (input.DaysRemaining < 0) riskFactors.Add("Past due date");
        else if (input.DaysRemaining <= 1) riskFactors.Add("Due today or tomorrow");
        else if (input.DaysRemaining <= 3) riskFactors.Add("Due date approaching");
        if (input.NumberOfStatusChanges > 2) riskFactors.Add("Multiple status changes");
        if (remainingHours > 20) riskFactors.Add("Large remaining workload");
        var priorityName = task.Priority;
        if (priorityName is "Medium" or "High" or "Critical") riskFactors.Add($"{priorityName} priority");

        var explanation = string.Join("; ", riskFactors.Count > 0 ? riskFactors : ["Healthy delivery outlook"]);

        var evaluation = new TaskRiskEvaluation
        {
            TaskId = input.TaskId,
            EstimateHours = input.EstimateHours,
            TaskAge = input.TaskAge,
            DaysRemaining = input.DaysRemaining,
            ProgressPercentage = input.ProgressPercentage,
            IsBlocked = input.IsBlocked,
            PriorityId = input.PriorityId,
            CurrentStatusId = input.CurrentStatusId,
            NumberOfStatusChanges = input.NumberOfStatusChanges,
            RiskScore = score,
            RiskLevel = level,
            Confidence = score / 100m,
            RiskFactors = explanation,
            Explanation = explanation,
            EvaluatedAt = DateTime.UtcNow,
            EvaluatedBy = evaluatedBy.ToString(),
            CreatedBy = evaluatedBy,
            IsActive = true,
            IsDeleted = false
        };

        await _riskRepository.AddAsync(evaluation);

        return new AiRiskEvaluationResultDto
        {
            TaskId = input.TaskId,
            RiskScore = score,
            RiskLevel = level,
            Confidence = score / 100m,
            RiskFactors = explanation,
            Explanation = explanation,
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<AiRiskEvaluationResultDto?> GetLatestAsync(int taskId)
    {
        var evaluation = await _riskRepository.GetLatestByTaskIdAsync(taskId);
        return evaluation == null ? null : new AiRiskEvaluationResultDto
        {
            TaskId = evaluation.TaskId,
            RiskScore = evaluation.RiskScore,
            RiskLevel = evaluation.RiskLevel,
            Confidence = evaluation.Confidence,
            RiskFactors = evaluation.RiskFactors,
            Explanation = evaluation.Explanation,
            Timestamp = evaluation.EvaluatedAt
        };
    }

    private static decimal CalculateRiskScore(AiRiskEvaluationInputDto dto, decimal remainingHours)
    {
        decimal score = 0m;

        if (dto.IsBlocked) score += 25m;
        if (dto.ProgressPercentage < 35) score += 20m;
        if (dto.DaysRemaining < 0) score += 25m;
        else if (dto.DaysRemaining <= 1) score += 25m;
        else if (dto.DaysRemaining <= 3) score += 15m;
        if (dto.NumberOfStatusChanges > 2) score += 15m;
        if (remainingHours > 20) score += 20m;

        score += dto.PriorityId switch
        {
            4 => 15m,
            3 => 10m,
            2 => 5m,
            _ => 0m
        };

        return Math.Min(100m, score);
    }
}
