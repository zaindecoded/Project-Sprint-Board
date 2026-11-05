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
        int take,
        int? managerId = null)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(x => x.Task)
            .Include(x => x.User)
            .Include(x => x.RoleInTaskDefinition)
            .Where(x => !x.IsDeleted)
            .AsQueryable();
        if (managerId.HasValue) query = query.Where(x => x.Task.Sprint.Project.CreatedBy == managerId.Value);

        if (taskId.HasValue)
            query = query.Where(x => x.TaskId == taskId.Value);

        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.RoleInTaskDefinition.Name.Contains(search)
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
        bool? isActive,
        int? managerId = null)
    {
        var query = DbSet.AsNoTracking().Where(x => !x.IsDeleted).AsQueryable();
        if (managerId.HasValue) query = query.Where(x => x.Task.Sprint.Project.CreatedBy == managerId.Value);

        if (taskId.HasValue)
            query = query.Where(x => x.TaskId == taskId.Value);

        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.RoleInTaskDefinition.Name.Contains(search)
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
            .Include(x => x.RoleInTaskDefinition)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    public Task<Assignment?> GetByIdForManagerAsync(int id, int managerId) => DbSet
        .Include(x => x.Task).ThenInclude(x => x.Sprint)
        .Include(x => x.User)
        .Include(x => x.RoleInTaskDefinition)
        .FirstOrDefaultAsync(x => x.Id == id && x.Task.Sprint.Project.CreatedBy == managerId && !x.IsDeleted);

    public async Task<bool> ExistsActiveAssignmentAsync(int taskId, int userId, int? excludeId = null)
    {
        var query = DbSet.AsNoTracking().Where(x => x.TaskId == taskId && x.UserId == userId && x.IsActive && !x.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);

        return await query.AnyAsync();
    }
}
