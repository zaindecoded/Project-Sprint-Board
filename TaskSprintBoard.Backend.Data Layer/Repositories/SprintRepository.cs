using Microsoft.EntityFrameworkCore;
using TaskSprintBoard.Backend.Data.DbContext;
using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.Data.Repositories;

public class SprintRepository : GenericRepository<Sprint>
{
    public SprintRepository(TaskSprintBoardDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Sprint>> SearchAsync(string? search, Guid? projectId, int skip, int take)
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

        return await query.OrderBy(x => x.StartDate).Skip(skip).Take(take).ToListAsync();
    }

    public async Task<bool> ExistsByNameForProjectAsync(Guid projectId, string name, Guid? excludeId = null)
    {
        var query = DbSet.AsNoTracking().Where(x => x.ProjectId == projectId && x.Name == name && !x.IsDeleted);

        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public Task<Sprint?> GetByIdWithProjectAsync(Guid id)
    {
        return DbSet.AsNoTracking()
            .Include(sprint => sprint.Project)
            .FirstOrDefaultAsync(sprint => sprint.Id == id && !sprint.IsDeleted);
    }

    public Task<bool> HasTaskDueAfterAsync(Guid sprintId, DateTime endDate)
    {
        return Context.Tasks.AsNoTracking().AnyAsync(task => task.SprintId == sprintId
            && !task.IsDeleted
            && task.DueDate.HasValue
            && task.DueDate.Value.Date > endDate.Date);
    }
}
