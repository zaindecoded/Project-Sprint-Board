using Microsoft.EntityFrameworkCore;
using TaskSprintBoard.Backend.Data.DbContext;
using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.Data.Repositories;

public class TaskRepository : GenericRepository<TaskItem>
{
    public TaskRepository(TaskSprintBoardDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<TaskItem>> SearchAsync(
        string? search,
        string? status,
        string? priority,
        Guid? projectId,
        Guid? sprintId,
        Guid? userId,
        DateTime? dueDate,
        bool? blocked,
        int skip,
        int take)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(x => x.Sprint)
            .ThenInclude(x => x.Project)
            .Include(x => x.Assignments)
            .ThenInclude(x => x.User)
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Title.Contains(search) || x.Description.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            query = query.Where(x => x.Priority == priority);
        }

        if (sprintId.HasValue)
        {
            query = query.Where(x => x.SprintId == sprintId.Value);
        }

        if (projectId.HasValue)
        {
            query = query.Where(x => x.Sprint.ProjectId == projectId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(x => x.Assignments.Any(a => a.UserId == userId.Value && a.IsActive && !a.IsDeleted));
        }

        if (dueDate.HasValue)
        {
            query = query.Where(x => x.DueDate.HasValue && x.DueDate.Value.Date == dueDate.Value.Date);
        }

        if (blocked.HasValue)
        {
            query = query.Where(x => (x.Status == "Blocked") == blocked.Value);
        }

        return await query.OrderBy(x => x.DueDate ?? DateTime.MaxValue).Skip(skip).Take(take).ToListAsync();
    }

    public async Task<int> CountAsync(
        string? search,
        string? status,
        string? priority,
        Guid? projectId,
        Guid? sprintId,
        Guid? userId,
        DateTime? dueDate,
        bool? blocked)
    {
        var query = DbSet.AsNoTracking().Where(x => !x.IsDeleted).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Title.Contains(search) || x.Description.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            query = query.Where(x => x.Priority == priority);
        }

        if (sprintId.HasValue)
        {
            query = query.Where(x => x.SprintId == sprintId.Value);
        }

        if (projectId.HasValue)
        {
            query = query.Where(x => x.Sprint.ProjectId == projectId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(x => x.Assignments.Any(a => a.UserId == userId.Value && a.IsActive && !a.IsDeleted));
        }

        if (dueDate.HasValue)
        {
            query = query.Where(x => x.DueDate.HasValue && x.DueDate.Value.Date == dueDate.Value.Date);
        }

        if (blocked.HasValue)
        {
            query = query.Where(x => (x.Status == "Blocked") == blocked.Value);
        }

        return await query.CountAsync();
    }

    public async Task<TaskItem?> GetByIdWithDetailsAsync(Guid id)
    {
        return await DbSet
            .Include(x => x.Sprint)
            .ThenInclude(x => x.Project)
            .Include(x => x.Assignments)
            .ThenInclude(x => x.User)
            .Include(x => x.Comments)
            .Include(x => x.StatusHistory)
            .Include(x => x.RiskEvaluations)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    public async Task<IEnumerable<TaskItem>> GetAllWithRiskEvaluationsAsync()
    {
        return await DbSet
            .AsNoTracking()
            .Include(x => x.RiskEvaluations)
            .Where(x => !x.IsDeleted)
            .ToListAsync();
    }

    public async Task<bool> ExistsByTitleInSprintAsync(Guid sprintId, string title, Guid? excludeId = null)
    {
        var query = DbSet.AsNoTracking().Where(x => x.SprintId == sprintId && x.Title == title && !x.IsDeleted);
        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> SprintExistsAsync(Guid sprintId)
    {
        return await Context.Set<Sprint>().AsNoTracking().AnyAsync(x => x.Id == sprintId && !x.IsDeleted);
    }

    public Task<bool> HasAssignmentAfterAsync(Guid taskId, DateTime dueDate)
    {
        return Context.Assignments.AsNoTracking().AnyAsync(assignment => assignment.TaskId == taskId
            && !assignment.IsDeleted
            && assignment.AssignedAt.Date > dueDate.Date);
    }

    public async Task<bool> IsAssignedToUserAsync(Guid taskId, Guid userId)
    {
        return await DbSet.AsNoTracking().AnyAsync(task => task.Id == taskId
            && !task.IsDeleted
            && task.Assignments.Any(assignment => assignment.UserId == userId && assignment.IsActive && !assignment.IsDeleted));
    }
}
