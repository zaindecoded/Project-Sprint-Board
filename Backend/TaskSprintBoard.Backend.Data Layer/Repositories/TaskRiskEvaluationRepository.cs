using Microsoft.EntityFrameworkCore;
using TaskSprintBoard.Backend.Data.DbContext;
using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.Data.Repositories;

public class TaskRiskEvaluationRepository : GenericRepository<TaskRiskEvaluation>
{
    public TaskRiskEvaluationRepository(TaskSprintBoardDbContext context) : base(context)
    {
    }

    public Task<TaskRiskEvaluation?> GetLatestByTaskIdAsync(int taskId)
    {
        return DbSet.AsNoTracking().Where(x => x.TaskId == taskId && !x.IsDeleted).OrderByDescending(x => x.EvaluatedAt).FirstOrDefaultAsync();
    }
}
