using Microsoft.EntityFrameworkCore;
using TaskSprintBoard.Backend.Data.DbContext;
using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.Data.Repositories;

public class AuditLogRepository
{
    private readonly TaskSprintBoardDbContext _context;
    public AuditLogRepository(TaskSprintBoardDbContext context) => _context = context;

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> SearchAsync(int page, int pageSize, string? search, string? action, string? entity)
    {
        var query = from log in _context.AuditLogs.AsNoTracking()
                    join user in _context.Users.AsNoTracking() on log.ActorUserId equals user.Id into actorJoin
                    from actor in actorJoin.DefaultIfEmpty()
                    select new { Log = log, Actor = actor };
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(x => x.Log.Action == action);
        if (!string.IsNullOrWhiteSpace(entity)) query = query.Where(x => x.Log.EntityType == entity);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Log.Action.Contains(term) || x.Log.EntityType.Contains(term)
                || (x.Log.ActorName ?? "").Contains(term)
                || (x.Actor != null && (x.Actor.FirstName.Contains(term) || x.Actor.LastName.Contains(term) || x.Actor.Email.Contains(term)))
                || (x.Log.Details ?? "").Contains(term));
        }
        var count = await query.CountAsync();
        var rows = await query.OrderByDescending(x => x.Log.OccurredAt).ThenByDescending(x => x.Log.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        foreach (var row in rows)
            row.Log.ActorName = row.Actor == null ? row.Log.ActorName : $"{row.Actor.FirstName} {row.Actor.LastName}".Trim();
        return (rows.Select(x => x.Log).ToList(), count);
    }

    public async Task RecordLoginAsync(int actorUserId, string actorName)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            OccurredAt = DateTime.UtcNow,
            ActorUserId = actorUserId,
            ActorName = actorName,
            Action = "Login",
            EntityType = "Authentication",
            Details = "{}"
        });
        await _context.SaveChangesAsync();
    }

    public async Task<(IReadOnlyList<string> Actions, IReadOnlyList<string> Entities)> GetFiltersAsync()
    {
        var actions = await _context.AuditLogs.AsNoTracking().Select(x => x.Action).Distinct().OrderBy(x => x).ToListAsync();
        var entities = await _context.AuditLogs.AsNoTracking().Select(x => x.EntityType).Distinct().OrderBy(x => x).ToListAsync();
        return (actions, entities);
    }
}
