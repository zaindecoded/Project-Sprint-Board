using TaskSprintBoard.Backend.BusinessLogic.DTOs;
using TaskSprintBoard.Backend.Data.Entities;
using TaskSprintBoard.Backend.Data.Repositories;

namespace TaskSprintBoard.Backend.BusinessLogic.Services;

public class AiRiskService
{
    private readonly TaskRiskEvaluationRepository _riskRepository;
    private readonly TaskRepository _taskRepository;

    public AiRiskService(TaskRiskEvaluationRepository riskRepository, TaskRepository taskRepository)
    {
        _riskRepository = riskRepository;
        _taskRepository = taskRepository;
    }

    public async Task<AiRiskEvaluationResultDto> EvaluateAsync(AiRiskEvaluationInputDto dto, string evaluatedBy)
    {
        if (dto.EstimateHours <= 0)
            throw new InvalidOperationException("EstimateHours must be greater than zero.");

        var task = await _taskRepository.GetByIdAsync(dto.TaskId) ?? throw new KeyNotFoundException("Task not found.");

        var score = CalculateRiskScore(dto);
        var level = score switch
        {
            >= 70 => "High",
            >= 50 => "Medium",
            _ => "Low"
        };

        var riskFactors = new List<string>();
        if (dto.IsBlocked) riskFactors.Add("Task is blocked");
        if (dto.ProgressPercentage < 35) riskFactors.Add("Low progress");
        if (dto.DaysRemaining <= 3) riskFactors.Add("Due date approaching");
        if (dto.NumberOfStatusChanges > 2) riskFactors.Add("Multiple status changes");
        if (dto.EstimateHours > 20) riskFactors.Add("Large remaining workload");
        if (dto.Priority is "High" or "Critical") riskFactors.Add($"{dto.Priority} priority");
        if (dto.CurrentStatus == "Blocked") riskFactors.Add("Task is blocked");

        var explanation = string.Join("; ", riskFactors.Count > 0 ? riskFactors : ["Healthy delivery outlook"]);

        var evaluation = new TaskRiskEvaluation
        {
            TaskId = dto.TaskId,
            EstimateHours = dto.EstimateHours,
            TaskAge = dto.TaskAge,
            DaysRemaining = dto.DaysRemaining,
            ProgressPercentage = dto.ProgressPercentage,
            IsBlocked = dto.IsBlocked,
            Priority = dto.Priority,
            CurrentStatus = dto.CurrentStatus,
            NumberOfStatusChanges = dto.NumberOfStatusChanges,
            RiskScore = score,
            RiskLevel = level,
            Confidence = score / 100m,
            RiskFactors = explanation,
            Explanation = explanation,
            EvaluatedAt = DateTime.UtcNow,
            EvaluatedBy = evaluatedBy,
            CreatedBy = evaluatedBy,
            IsActive = true,
            IsDeleted = false
        };

        await _riskRepository.AddAsync(evaluation);

        return new AiRiskEvaluationResultDto
        {
            TaskId = dto.TaskId,
            RiskScore = score,
            RiskLevel = level,
            Confidence = score / 100m,
            RiskFactors = explanation,
            Explanation = explanation,
            Timestamp = DateTime.UtcNow
        };
    }

    private static decimal CalculateRiskScore(AiRiskEvaluationInputDto dto)
    {
        decimal score = 0m;

        if (dto.IsBlocked) score += 25m;
        if (dto.ProgressPercentage < 35) score += 20m;
        if (dto.DaysRemaining <= 3) score += 15m;
        if (dto.NumberOfStatusChanges > 2) score += 15m;
        if (dto.EstimateHours > 20) score += 20m;

        score += dto.Priority switch
        {
            "Critical" => 15m,
            "High" => 10m,
            "Medium" => 5m,
            _ => 0m
        };

        score += dto.CurrentStatus switch
        {
            "Blocked" => 10m,
            "InProgress" => 5m,
            _ => 0m
        };

        return Math.Min(100m, score);
    }
}
