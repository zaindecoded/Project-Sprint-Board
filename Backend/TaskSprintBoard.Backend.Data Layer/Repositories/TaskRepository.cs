using Microsoft.EntityFrameworkCore;
using TaskSprintBoard.Backend.Data.DbContext;
using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.Data.Repositories;

public class TaskRepository : GenericRepository<TaskItem>
{
    public TaskRepository(TaskSprintBoardDbContext context) : base(context)
    {
    }

    public override Task<TaskItem?> GetByIdAsync(int id) => DbSet.Include(x => x.PriorityDefinition).Include(x => x.StatusDefinition).FirstOrDefaultAsync(x => x.Id == id);

    public Task<TaskItem?> GetByIdForManagerAsync(int id, int managerId) => DbSet
        .Include(x => x.PriorityDefinition).Include(x => x.StatusDefinition).Include(x => x.Sprint)
        .FirstOrDefaultAsync(x => x.Id == id && x.Sprint.Project.CreatedBy == managerId && !x.IsDeleted);

    public async Task<IEnumerable<TaskItem>> SearchAsync(
        string? search,
        string? status,
        string? priority,
        int? projectId,
        int? sprintId,
        int? userId,
        DateTime? dueDate,
        string? dueDatePreset,
        bool? blocked,
        int skip,
        int take,
        int? managerId = null)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(x => x.PriorityDefinition)
            .Include(x => x.StatusDefinition)
            .Include(x => x.Sprint)
            .ThenInclude(x => x.Project)
            .Include(x => x.Assignments)
            .ThenInclude(x => x.User)
            .Where(x => !x.IsDeleted)
            .AsQueryable();
        if (managerId.HasValue) query = query.Where(x => x.Sprint.Project.CreatedBy == managerId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Title.Contains(search) || x.Description.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.StatusDefinition.Name == status);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            query = query.Where(x => x.PriorityDefinition.Name == priority);
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

        query = ApplyDueDatePreset(query, dueDatePreset);

        if (blocked.HasValue)
        {
            query = query.Where(x => (x.StatusDefinition.Name == "Blocked") == blocked.Value);
        }

        return await query.OrderBy(x => x.DueDate ?? DateTime.MaxValue).Skip(skip).Take(take).ToListAsync();
    }

    public async Task<int> CountAsync(
        string? search,
        string? status,
        string? priority,
        int? projectId,
        int? sprintId,
        int? userId,
        DateTime? dueDate,
        string? dueDatePreset,
        bool? blocked,
        int? managerId = null)
    {
        var query = DbSet.AsNoTracking().Where(x => !x.IsDeleted).AsQueryable();
        if (managerId.HasValue) query = query.Where(x => x.Sprint.Project.CreatedBy == managerId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Title.Contains(search) || x.Description.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.StatusDefinition.Name == status);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            query = query.Where(x => x.PriorityDefinition.Name == priority);
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

        query = ApplyDueDatePreset(query, dueDatePreset);

        if (blocked.HasValue)
        {
            query = query.Where(x => (x.StatusDefinition.Name == "Blocked") == blocked.Value);
        }

        return await query.CountAsync();
    }

    private static IQueryable<TaskItem> ApplyDueDatePreset(IQueryable<TaskItem> query, string? preset)
    {
        var today = DateTime.Today;
        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        return preset switch
        {
            "overdue" => query.Where(x => x.DueDate.HasValue && x.DueDate.Value.Date < today && x.StatusDefinition.Name != "Done" && x.StatusDefinition.Name != "Cancelled"),
            "today" => query.Where(x => x.DueDate.HasValue && x.DueDate.Value.Date == today),
            "thisWeek" => query.Where(x => x.DueDate.HasValue && x.DueDate.Value.Date >= today && x.DueDate.Value.Date < weekStart.AddDays(7)),
            "noDueDate" => query.Where(x => !x.DueDate.HasValue),
            _ => query
        };
    }

    public async Task<TaskItem?> GetByIdWithDetailsAsync(int id)
    {
        return await DbSet
            .Include(x => x.PriorityDefinition)
            .Include(x => x.StatusDefinition)
            .Include(x => x.Sprint)
            .ThenInclude(x => x.Project)
            .Include(x => x.Assignments)
            .ThenInclude(x => x.User)
            .Include(x => x.Comments)
            .Include(x => x.StatusHistory)
            .Include(x => x.RiskEvaluations)
            .Include(x => x.PriorityDefinition)
            .Include(x => x.StatusDefinition)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    public async Task<IEnumerable<TaskItem>> GetAllWithRiskEvaluationsAsync(int? assignedUserId = null, int? managerId = null)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(x => x.RiskEvaluations)
            .Include(x => x.PriorityDefinition)
            .Include(x => x.StatusDefinition)
            .Include(x => x.Sprint).ThenInclude(x => x.Project)
            .Where(x => !x.IsDeleted);

        if (managerId.HasValue)
            query = query.Where(x => x.Sprint.Project.CreatedBy == managerId.Value);

        if (assignedUserId.HasValue)
            query = query.Where(x => x.Assignments.Any(assignment => assignment.UserId == assignedUserId.Value
                && assignment.IsActive && !assignment.IsDeleted));

        return await query.ToListAsync();
    }

    public async Task<bool> ExistsByTitleInSprintAsync(int sprintId, string title, int? excludeId = null)
    {
        var query = DbSet.AsNoTracking().Where(x => x.SprintId == sprintId && x.Title == title && !x.IsDeleted);
        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> SprintExistsAsync(int sprintId)
    {
        return await Context.Set<Sprint>().AsNoTracking().AnyAsync(x => x.Id == sprintId && !x.IsDeleted);
    }

    public Task<bool> HasAssignmentAfterAsync(int taskId, DateTime dueDate)
    {
        return Context.Assignments.AsNoTracking().AnyAsync(assignment => assignment.TaskId == taskId
            && !assignment.IsDeleted
            && assignment.AssignedAt.Date > dueDate.Date);
    }

    public async Task<bool> IsAssignedToUserAsync(int taskId, int userId)
    {
        return await DbSet.AsNoTracking().AnyAsync(task => task.Id == taskId
            && !task.IsDeleted
            && task.Assignments.Any(assignment => assignment.UserId == userId && assignment.IsActive && !assignment.IsDeleted));
    }

    public Task<TaskPriorityLookup?> GetPriorityAsync(string name) => Context.TaskPriorities.FirstOrDefaultAsync(x => x.Name == name);
    public Task<TaskStatusLookup?> GetStatusAsync(string name) => Context.TaskStatuses.FirstOrDefaultAsync(x => x.Name == name);
}
