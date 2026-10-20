using Microsoft.EntityFrameworkCore;
using TaskSprintBoard.Backend.Data.DbContext;
using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.Data.Repositories;

public class CommentRepository : GenericRepository<Comment>
{
    public CommentRepository(TaskSprintBoardDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Comment>> SearchAsync(
        string? search,
        int? taskId,
        int? userId,
        int? assignedToUserId,
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

        if (assignedToUserId.HasValue)
            query = query.Where(x => x.Task.Assignments.Any(assignment => assignment.UserId == assignedToUserId.Value && assignment.IsActive && !assignment.IsDeleted));

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Message.Contains(search)
                || x.Task.Title.Contains(search)
                || x.User.FirstName.Contains(search)
                || x.User.LastName.Contains(search)
                || x.User.Email.Contains(search));
        }

        return await query.OrderByDescending(x => x.CreatedAt).Skip(skip).Take(take).ToListAsync();
    }

    public async Task<int> CountAsync(string? search, int? taskId, int? userId, int? assignedToUserId)
    {
        var query = DbSet.AsNoTracking().Where(x => !x.IsDeleted).AsQueryable();

        if (taskId.HasValue)
            query = query.Where(x => x.TaskId == taskId.Value);

        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId.Value);

        if (assignedToUserId.HasValue)
            query = query.Where(x => x.Task.Assignments.Any(assignment => assignment.UserId == assignedToUserId.Value && assignment.IsActive && !assignment.IsDeleted));

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Message.Contains(search)
                || x.Task.Title.Contains(search)
                || x.User.FirstName.Contains(search)
                || x.User.LastName.Contains(search)
                || x.User.Email.Contains(search));
        }

        return await query.CountAsync();
    }

    public async Task<Comment?> GetByIdWithDetailsAsync(int id)
    {
        return await DbSet
            .Include(x => x.Task)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    public async Task<IEnumerable<Comment>> GetByTaskIdAsync(int taskId)
    {
        return await DbSet.AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.TaskId == taskId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }
}
