using TaskSprintBoard.Backend.BusinessLogic.DTOs;
using TaskSprintBoard.Backend.Data.Entities;
using TaskSprintBoard.Backend.Data.Repositories;

namespace TaskSprintBoard.Backend.BusinessLogic.Services;

public class DashboardService
{
    private readonly ProjectRepository _projectRepository;
    private readonly SprintRepository _sprintRepository;
    private readonly TaskRepository _taskRepository;

    public DashboardService(ProjectRepository projectRepository, SprintRepository sprintRepository, TaskRepository taskRepository)
    {
        _projectRepository = projectRepository;
        _sprintRepository = sprintRepository;
        _taskRepository = taskRepository;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(int? assignedUserId = null)
    {
        var projects = (await _projectRepository.GetAllAsync()).Where(x => !x.IsDeleted).ToList();
        var sprints = (await _sprintRepository.GetAllAsync()).Where(x => !x.IsDeleted).ToList();
        var tasks = (await _taskRepository.GetAllWithRiskEvaluationsAsync(assignedUserId)).ToList();

        var now = DateTime.UtcNow;

        return new DashboardSummaryDto
        {
            TotalProjects = assignedUserId.HasValue ? 0 : projects.Count,
            CompletedProjects = assignedUserId.HasValue ? 0 : projects.Count(x => string.Equals(x.Status, "Completed", StringComparison.OrdinalIgnoreCase)),
            ActiveProjects = assignedUserId.HasValue ? 0 : projects.Count(x => x.IsActive && x.Status == "Active"),
            ActiveSprints = assignedUserId.HasValue ? 0 : sprints.Count(x => x.IsActive && x.Status == "Active"),
            TotalTasks = tasks.Count,
            TodoTasks = CountStatus(tasks, "Todo"),
            InProgressTasks = CountStatus(tasks, "InProgress"),
            CompletedTasks = CountStatus(tasks, "Done"),
            OverdueTasks = tasks.Count(x => x.DueDate.HasValue && x.DueDate.Value < now && x.Status != "Done"),
            BlockedTasks = CountStatus(tasks, "Blocked"),
            LowRiskTasks = tasks.Count(task => IsOpen(task) && LatestRisk(task)?.RiskLevel == "Low"),
            MediumRiskTasks = tasks.Count(task => IsOpen(task) && LatestRisk(task)?.RiskLevel == "Medium"),
            CriticalTasks = tasks.Count(task => IsOpen(task) && task.Priority == "Critical"),
            HighRiskTasks = tasks.Count(task => IsOpen(task) && LatestRisk(task)?.RiskLevel == "High")
        };
    }

    public async Task<IReadOnlyList<DashboardAttentionTaskDto>> GetAttentionTasksAsync(int? assignedUserId = null)
    {
        var tasks = await _taskRepository.GetAllWithRiskEvaluationsAsync(assignedUserId);
        var now = DateTime.UtcNow;

        return tasks
            .Where(IsOpen)
            .Select(task => new { Task = task, Risk = LatestRisk(task) })
            .Where(item => item.Task.Priority == "Critical" || item.Risk?.RiskLevel == "High")
            .OrderByDescending(item => item.Task.DueDate.HasValue && item.Task.DueDate.Value < now)
            .ThenByDescending(item => item.Risk?.RiskLevel == "High")
            .ThenByDescending(item => item.Task.Priority == "Critical")
            .ThenBy(item => item.Task.DueDate ?? DateTime.MaxValue)
            .Take(10)
            .Select(item => new DashboardAttentionTaskDto
            {
                Id = item.Task.Id,
                Title = item.Task.Title,
                Priority = item.Task.Priority,
                Status = item.Task.Status,
                DueDate = item.Task.DueDate,
                RiskLevel = item.Risk?.RiskLevel,
                RiskScore = item.Risk?.RiskScore,
                RiskFactors = item.Risk?.RiskFactors
            })
            .ToList();
    }

    private static TaskRiskEvaluation? LatestRisk(TaskItem task)
    {
        return task.RiskEvaluations
            .Where(evaluation => !evaluation.IsDeleted)
            .OrderByDescending(evaluation => evaluation.EvaluatedAt)
            .FirstOrDefault();
    }

    private static int CountStatus(IEnumerable<TaskItem> tasks, string status)
    {
        static string Normalize(string? value) => new string((value ?? string.Empty).Where(character => !char.IsWhiteSpace(character)).ToArray());
        return tasks.Count(task => string.Equals(Normalize(task.Status), status, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsOpen(TaskItem task)
    {
        return task.Status != "Done" && task.Status != "Cancelled";
    }
}
