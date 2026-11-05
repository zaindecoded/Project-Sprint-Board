using Microsoft.EntityFrameworkCore;
using TaskSprintBoard.Backend.Data.DbContext;
using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.Data.Repositories;

public class ProjectRepository : GenericRepository<Project>
{
    public ProjectRepository(TaskSprintBoardDbContext context) : base(context)
    {
    }

    public override Task<Project?> GetByIdAsync(int id) => DbSet.Include(x => x.StatusDefinition).FirstOrDefaultAsync(x => x.Id == id);

    public async Task<Project?> GetByNameAsync(string name)
    {
        return await DbSet.AsNoTracking().Include(x => x.StatusDefinition).FirstOrDefaultAsync(x => x.Name == name && !x.IsDeleted);
    }

    private IQueryable<Project> SearchQuery(string? search, string? status, int? ownerUserId = null)
    {
        var query = DbSet.AsNoTracking().Include(x => x.StatusDefinition).Where(x => !x.IsDeleted).AsQueryable();
        if (ownerUserId.HasValue) query = query.Where(x => x.CreatedBy == ownerUserId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Name.Contains(search) || (x.Description != null && x.Description.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                var today = DateTime.UtcNow.Date;
                query = query.Where(x => x.StatusDefinition.Name == "Active" && (!x.EndDate.HasValue || x.EndDate.Value.Date >= today));
            }
            else
            {
                query = query.Where(x => x.StatusDefinition.Name == status);
            }
        }

        return query;
    }

    public async Task<IEnumerable<Project>> SearchAsync(string? search, string? status, string? sortBy, int skip, int take, int? ownerUserId = null)
    {
        var query = SearchQuery(search, status, ownerUserId);
        query = sortBy?.ToLowerInvariant() switch
        {
            "name" => query.OrderBy(x => x.Name),
            "enddate" => query.OrderBy(x => x.EndDate ?? DateTime.MaxValue).ThenBy(x => x.Name),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };

        return await query.Skip(skip).Take(take).ToListAsync();
    }

    public Task<int> CountSearchAsync(string? search, string? status, int? ownerUserId = null) => SearchQuery(search, status, ownerUserId).CountAsync();
    public Task<Project?> GetOwnedByAsync(int id, int ownerUserId) => DbSet.AsNoTracking().Include(x => x.StatusDefinition).FirstOrDefaultAsync(x => x.Id == id && x.CreatedBy == ownerUserId && !x.IsDeleted);
    public Task<List<Project>> GetAllOwnedByAsync(int ownerUserId) => DbSet.AsNoTracking().Include(x => x.StatusDefinition).Where(x => x.CreatedBy == ownerUserId && !x.IsDeleted).ToListAsync();

    public Task<bool> HasSprintEndingAfterAsync(int projectId, DateTime endDate)
    {
        return Context.Sprints.AsNoTracking().AnyAsync(sprint => sprint.ProjectId == projectId
            && !sprint.IsDeleted
            && sprint.EndDate.Date > endDate.Date);
    }

    public async Task UpdateAndCompleteWorkAsync(Project project, int updatedBy)
    {
        var now = DateTime.UtcNow;
        Context.Projects.Update(project);

        var sprints = await Context.Sprints
            .Where(sprint => sprint.ProjectId == project.Id && !sprint.IsDeleted)
            .ToListAsync();
        var sprintIds = sprints.Select(sprint => sprint.Id).ToArray();
        var tasks = await Context.Tasks
            .Include(task => task.StatusDefinition)
            .Where(task => sprintIds.Contains(task.SprintId) && !task.IsDeleted)
            .ToListAsync();

        foreach (var sprint in sprints)
        {
            if (sprint.StatusId != 4)
            {
                sprint.StatusId = 4;
                sprint.UpdatedAt = now;
                sprint.UpdatedBy = updatedBy;
            }
        }

        foreach (var task in tasks)
        {
            if (string.Equals(task.Status, "Done", StringComparison.OrdinalIgnoreCase))
                continue;

            var oldStatusId = task.StatusId;
            task.StatusId = 5;
            task.StatusDefinition = await Context.TaskStatuses.FirstAsync(x => x.Id == 5);
            task.ProgressPercentage = 100;
            task.UpdatedAt = now;
            task.UpdatedBy = updatedBy;

            Context.StatusHistory.Add(new StatusHistory
            {
                TaskId = task.Id,
                OldStatusId = oldStatusId,
                NewStatusId = 5,
                ChangedBy = updatedBy.ToString(),
                ChangedAt = now,
                Reason = "Automatically completed because the parent project was marked completed.",
                CreatedAt = now,
                CreatedBy = updatedBy,
                IsActive = true,
                IsDeleted = false
            });
        }

        await Context.SaveChangesAsync();
    }
}
