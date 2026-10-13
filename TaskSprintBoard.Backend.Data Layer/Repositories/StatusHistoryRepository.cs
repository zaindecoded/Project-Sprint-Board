using Microsoft.EntityFrameworkCore;
using TaskSprintBoard.Backend.Data.DbContext;
using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.Data.Repositories;

public class StatusHistoryRepository : GenericRepository<StatusHistory>
{
    public StatusHistoryRepository(TaskSprintBoardDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<StatusHistory>> SearchAsync(
        Guid? taskId,
        string? oldStatus,
        string? newStatus,
        int skip,
        int take)
    {
        var query = DbSet
            .AsNoTracking()
            .Include(x => x.Task)
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (taskId.HasValue)
            query = query.Where(x => x.TaskId == taskId.Value);

        if (!string.IsNullOrWhiteSpace(oldStatus))
            query = query.Where(x => x.OldStatus == oldStatus);

        if (!string.IsNullOrWhiteSpace(newStatus))
            query = query.Where(x => x.NewStatus == newStatus);

        return await query.OrderByDescending(x => x.ChangedAt).Skip(skip).Take(take).ToListAsync();
    }

    public async Task<int> CountAsync(Guid? taskId, string? oldStatus, string? newStatus)
    {
        var query = DbSet.AsNoTracking().Where(x => !x.IsDeleted).AsQueryable();

        if (taskId.HasValue)
            query = query.Where(x => x.TaskId == taskId.Value);

        if (!string.IsNullOrWhiteSpace(oldStatus))
            query = query.Where(x => x.OldStatus == oldStatus);

        if (!string.IsNullOrWhiteSpace(newStatus))
            query = query.Where(x => x.NewStatus == newStatus);

        return await query.CountAsync();
    }

    public async Task<IEnumerable<StatusHistory>> GetByTaskIdAsync(Guid taskId)
    {
        return await DbSet.AsNoTracking()
            .Where(x => x.TaskId == taskId && !x.IsDeleted)
            .OrderByDescending(x => x.ChangedAt)
            .ToListAsync();
    }

    public async Task<StatusHistory?> GetByIdWithDetailsAsync(Guid id)
    {
        return await DbSet
            .Include(x => x.Task)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }
}
