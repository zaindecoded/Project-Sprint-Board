using Microsoft.EntityFrameworkCore;
using TaskSprintBoard.Backend.Data.DbContext;
using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.Data.Repositories;

public class ProjectRepository : GenericRepository<Project>
{
    public ProjectRepository(TaskSprintBoardDbContext context) : base(context)
    {
    }

    public async Task<Project?> GetByNameAsync(string name)
    {
        return await DbSet.AsNoTracking().FirstOrDefaultAsync(x => x.Name == name && !x.IsDeleted);
    }

    private IQueryable<Project> SearchQuery(string? search, string? status)
    {
        var query = DbSet.AsNoTracking().Where(x => !x.IsDeleted).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Name.Contains(search) || (x.Description != null && x.Description.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                var today = DateTime.UtcNow.Date;
                query = query.Where(x => x.Status == "Active" && (!x.EndDate.HasValue || x.EndDate.Value.Date >= today));
            }
            else
            {
                query = query.Where(x => x.Status == status);
            }
        }

        return query;
    }

    public async Task<IEnumerable<Project>> SearchAsync(string? search, string? status, string? sortBy, int skip, int take)
    {
        var query = SearchQuery(search, status);
        query = sortBy?.ToLowerInvariant() switch
        {
            "name" => query.OrderBy(x => x.Name),
            "enddate" => query.OrderBy(x => x.EndDate ?? DateTime.MaxValue).ThenBy(x => x.Name),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };

        return await query.Skip(skip).Take(take).ToListAsync();
    }

    public Task<int> CountSearchAsync(string? search, string? status) => SearchQuery(search, status).CountAsync();

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
            .Where(task => sprintIds.Contains(task.SprintId) && !task.IsDeleted)
            .ToListAsync();

        foreach (var sprint in sprints)
        {
            if (!string.Equals(sprint.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                sprint.Status = "Completed";
                sprint.UpdatedAt = now;
                sprint.UpdatedBy = updatedBy;
            }
        }

        foreach (var task in tasks)
        {
            if (string.Equals(task.Status, "Done", StringComparison.OrdinalIgnoreCase))
                continue;

            var oldStatus = task.Status;
            task.Status = "Done";
            task.ProgressPercentage = 100;
            task.UpdatedAt = now;
            task.UpdatedBy = updatedBy;

            Context.StatusHistory.Add(new StatusHistory
            {
                TaskId = task.Id,
                OldStatus = oldStatus,
                NewStatus = "Done",
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
