using Microsoft.EntityFrameworkCore;
using TaskSprintBoard.Backend.Data.DbContext;
using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.Data.Repositories;

public class AssignmentRepository : GenericRepository<Assignment>
{
    public AssignmentRepository(TaskSprintBoardDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Assignment>> SearchAsync(
        string? search,
        int? taskId,
        int? userId,
        bool? isActive,
        string? sortBy,
        int skip,
        int take)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(x => x.Task)
            .Include(x => x.User)
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (taskId.HasValue)
            query = query.Where(x => x.TaskId == taskId.Value);

        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.RoleInTask != null && x.RoleInTask.Contains(search)
                || x.Task.Title.Contains(search)
                || x.User.FirstName.Contains(search)
                || x.User.LastName.Contains(search)
                || x.User.Email.Contains(search));
        }

        query = sortBy?.ToLowerInvariant() switch
        {
            "task" => query.OrderBy(x => x.Task.Title),
            "user" => query.OrderBy(x => x.User.FirstName).ThenBy(x => x.User.LastName),
            "assignedat" => query.OrderBy(x => x.AssignedAt),
            "-task" => query.OrderByDescending(x => x.Task.Title),
            "-user" => query.OrderByDescending(x => x.User.FirstName).ThenByDescending(x => x.User.LastName),
            "-assignedat" => query.OrderByDescending(x => x.AssignedAt),
            _ => query.OrderBy(x => x.AssignedAt)
        };
        return await query.Skip(skip).Take(take).ToListAsync();
    }

    public async Task<int> CountAsync(
        string? search,
        int? taskId,
        int? userId,
        bool? isActive)
    {
        var query = DbSet.AsNoTracking().Where(x => !x.IsDeleted).AsQueryable();

        if (taskId.HasValue)
            query = query.Where(x => x.TaskId == taskId.Value);

        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.RoleInTask != null && x.RoleInTask.Contains(search)
                || x.Task.Title.Contains(search)
                || x.User.FirstName.Contains(search)
                || x.User.LastName.Contains(search)
                || x.User.Email.Contains(search));
        }

        return await query.CountAsync();
    }

    public async Task<Assignment?> GetByIdWithDetailsAsync(int id)
    {
        return await DbSet
            .Include(x => x.Task)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    public async Task<IEnumerable<Assignment>> GetByTaskIdAsync(int taskId)
    {
        return await DbSet.AsNoTracking().Where(x => x.TaskId == taskId && !x.IsDeleted).Include(x => x.User).ToListAsync();
    }

    public async Task<bool> ExistsActiveAssignmentAsync(int taskId, int userId, int? excludeId = null)
    {
        var query = DbSet.AsNoTracking().Where(x => x.TaskId == taskId && x.UserId == userId && x.IsActive && !x.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);

        return await query.AnyAsync();
    }
}
