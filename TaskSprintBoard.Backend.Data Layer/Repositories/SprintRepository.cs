using Microsoft.EntityFrameworkCore;
using TaskSprintBoard.Backend.Data.DbContext;
using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.Data.Repositories;

public class SprintRepository : GenericRepository<Sprint>
{
    public SprintRepository(TaskSprintBoardDbContext context) : base(context)
    {
    }

    private IQueryable<Sprint> SearchQuery(string? search, int? projectId, string? status)
    {
        var query = DbSet.AsNoTracking().Where(x => !x.IsDeleted).AsQueryable();

        if (projectId.HasValue)
        {
            query = query.Where(x => x.ProjectId == projectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Name.Contains(search) || x.Goal.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);

        if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            var today = DateTime.Today;
            query = query.Where(x => x.EndDate.Date >= today);
        }

        return query;
    }

    public async Task<IEnumerable<Sprint>> SearchAsync(string? search, int? projectId, string? status, int skip, int take) =>
        await SearchQuery(search, projectId, status).OrderBy(x => x.StartDate).Skip(skip).Take(take).ToListAsync();

    public Task<int> CountSearchAsync(string? search, int? projectId, string? status) => SearchQuery(search, projectId, status).CountAsync();

    public async Task<bool> ExistsByNameForProjectAsync(int projectId, string name, int? excludeId = null)
    {
        var query = DbSet.AsNoTracking().Where(x => x.ProjectId == projectId && x.Name == name && !x.IsDeleted);

        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public Task<Sprint?> GetByIdWithProjectAsync(int id)
    {
        return DbSet.AsNoTracking()
            .Include(sprint => sprint.Project)
            .FirstOrDefaultAsync(sprint => sprint.Id == id && !sprint.IsDeleted);
    }

    public Task<bool> HasTaskDueAfterAsync(int sprintId, DateTime endDate)
    {
        return Context.Tasks.AsNoTracking().AnyAsync(task => task.SprintId == sprintId
            && !task.IsDeleted
            && task.DueDate.HasValue
            && task.DueDate.Value.Date > endDate.Date);
    }
}
