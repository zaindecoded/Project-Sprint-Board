using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.BusinessLogic.Interfaces;

public interface ITaskRiskEvaluationRepository
{
    Task<IEnumerable<TaskRiskEvaluation>> GetByTaskIdAsync(Guid taskId);
    Task<TaskRiskEvaluation> AddAsync(TaskRiskEvaluation evaluation);
}
