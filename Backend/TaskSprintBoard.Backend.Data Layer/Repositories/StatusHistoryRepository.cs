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
        int? taskId,
        int? oldStatusId,
        int? newStatusId,
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

        if (oldStatusId.HasValue)
            query = query.Where(x => x.OldStatusId == oldStatusId.Value);

        if (newStatusId.HasValue)
            query = query.Where(x => x.NewStatusId == newStatusId.Value);

        return await query.OrderByDescending(x => x.ChangedAt).Skip(skip).Take(take).ToListAsync();
    }

    public async Task<int> CountAsync(int? taskId, int? oldStatusId, int? newStatusId)
    {
        var query = DbSet.AsNoTracking().Where(x => !x.IsDeleted).AsQueryable();

        if (taskId.HasValue)
            query = query.Where(x => x.TaskId == taskId.Value);

        if (oldStatusId.HasValue)
            query = query.Where(x => x.OldStatusId == oldStatusId.Value);

        if (newStatusId.HasValue)
            query = query.Where(x => x.NewStatusId == newStatusId.Value);

        return await query.CountAsync();
    }

    public async Task<IEnumerable<StatusHistory>> GetByTaskIdAsync(int taskId)
    {
        return await DbSet.AsNoTracking()
            .Where(x => x.TaskId == taskId && !x.IsDeleted)
            .OrderByDescending(x => x.ChangedAt)
            .ToListAsync();
    }

    public async Task<StatusHistory?> GetByIdWithDetailsAsync(int id)
    {
        return await DbSet
            .Include(x => x.Task)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }
}
