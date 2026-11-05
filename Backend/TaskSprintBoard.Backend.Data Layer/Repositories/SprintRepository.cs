using Microsoft.EntityFrameworkCore;
using TaskSprintBoard.Backend.Data.DbContext;
using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.Data.Repositories;

public class SprintRepository : GenericRepository<Sprint>
{
    public SprintRepository(TaskSprintBoardDbContext context) : base(context)
    {
    }

    public override Task<Sprint?> GetByIdAsync(int id) => DbSet.Include(x => x.StatusDefinition).FirstOrDefaultAsync(x => x.Id == id);

    private IQueryable<Sprint> SearchQuery(string? search, int? projectId, string? status, int? managerId = null)
    {
        var query = DbSet.AsNoTracking().Include(x => x.StatusDefinition).Where(x => !x.IsDeleted).AsQueryable();
        if (managerId.HasValue) query = query.Where(x => x.Project.CreatedBy == managerId.Value);

        if (projectId.HasValue)
        {
            query = query.Where(x => x.ProjectId == projectId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Name.Contains(search) || x.Goal.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.StatusDefinition.Name == status);

        if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            var today = DateTime.Today;
            query = query.Where(x => x.EndDate.Date >= today);
        }

        return query;
    }

    public async Task<IEnumerable<Sprint>> SearchAsync(string? search, int? projectId, string? status, int skip, int take, int? managerId = null) =>
        await SearchQuery(search, projectId, status, managerId).OrderBy(x => x.StartDate).Skip(skip).Take(take).ToListAsync();

    public Task<int> CountSearchAsync(string? search, int? projectId, string? status, int? managerId = null) => SearchQuery(search, projectId, status, managerId).CountAsync();
    public Task<List<Sprint>> GetAllForProjectManagerAsync(int managerId) => DbSet.AsNoTracking().Include(x => x.StatusDefinition).Where(x => x.Project.CreatedBy == managerId && !x.IsDeleted).ToListAsync();

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

    public Task<Sprint?> GetByIdForManagerAsync(int id, int managerId) => DbSet.AsNoTracking()
        .Include(x => x.Project).FirstOrDefaultAsync(x => x.Id == id && x.Project.CreatedBy == managerId && !x.IsDeleted);

    public Task<bool> HasTaskDueAfterAsync(int sprintId, DateTime endDate)
    {
        return Context.Tasks.AsNoTracking().AnyAsync(task => task.SprintId == sprintId
            && !task.IsDeleted
            && task.DueDate.HasValue
            && task.DueDate.Value.Date > endDate.Date);
    }
}
