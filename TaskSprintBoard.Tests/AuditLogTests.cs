using Microsoft.EntityFrameworkCore;
using TaskSprintBoard.Backend.BusinessLogic.Services;
using TaskSprintBoard.Backend.Data.DbContext;
using TaskSprintBoard.Backend.Data.Entities;
using TaskSprintBoard.Backend.Data.Repositories;

namespace TaskSprintBoard.Tests;

public class AuditLogTests
{
    private static TaskSprintBoardDbContext CreateContext() => new(
        new DbContextOptionsBuilder<TaskSprintBoardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task SuccessfulEntityChangesAreLoggedWithoutCredentialFields()
    {
        await using var context = CreateContext();
        context.Users.Add(new User
        {
            FirstName = "Ava", LastName = "Member", Email = "ava@example.test", UserName = "ava",
            PasswordHash = "must-never-appear", CreatedBy = 7
        });

        await context.SaveChangesAsync();

        var log = await context.AuditLogs.SingleAsync();
        Assert.Equal("Created", log.Action);
        Assert.Equal("User", log.EntityType);
        Assert.Equal(7, log.ActorUserId);
        Assert.DoesNotContain("must-never-appear", log.Details);
        Assert.DoesNotContain("Password", log.Details, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuditSearchSupportsPaginationAndLoginEvents()
    {
        await using var context = CreateContext();
        context.Users.Add(new User { FirstName = "Sam", LastName = "Admin", Email = "sam@example.test", UserName = "sam" });
        await context.SaveChangesAsync();
        var repository = new AuditLogRepository(context);
        await repository.RecordLoginAsync(1, "Sam Admin");
        await repository.RecordLoginAsync(1, "Sam Admin");
        var service = new AuditLogService(repository);

        var result = await service.SearchAsync(page: 2, pageSize: 1, search: "Sam", action: "Login", entity: "Authentication");

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Single(result.Items);
        Assert.Equal("Sam Admin", result.Items[0].Actor);
    }

    [Fact]
    public async Task TaskStatusAndAssignmentChangesUseSpecificAuditActions()
    {
        await using var context = CreateContext();
        var project = new Project { Name = "Audit project", Status = "Active" };
        context.Projects.Add(project);
        await context.SaveChangesAsync();
        var sprint = new Sprint { ProjectId = project.Id, Name = "Sprint 1", Status = "Active" };
        context.Sprints.Add(sprint);
        await context.SaveChangesAsync();
        var task = new TaskItem { SprintId = sprint.Id, Title = "Review", Status = "Todo", Priority = "Medium" };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        task.Status = "In Progress";
        task.UpdatedBy = 1;
        await context.SaveChangesAsync();
        context.Assignments.Add(new Assignment { TaskId = task.Id, UserId = 1, CreatedBy = 1 });
        await context.SaveChangesAsync();

        Assert.Contains(await context.AuditLogs.ToListAsync(), x => x.EntityType == "Task" && x.Action == "Status Changed");
        Assert.Contains(await context.AuditLogs.ToListAsync(), x => x.EntityType == "Assignment" && x.Action == "Assigned");
    }
}
