using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TaskSprintBoard.Backend.BusinessLogic.DTOs;
using TaskSprintBoard.Backend.BusinessLogic.Services;
using TaskSprintBoard.Backend.Data.DbContext;
using TaskSprintBoard.Backend.Data.Entities;
using TaskSprintBoard.Backend.Data.Repositories;

namespace TaskSprintBoard.Tests;

public class ServiceTests
{
    private static TaskSprintBoardDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TaskSprintBoardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TaskSprintBoardDbContext(options);
    }

    private static async Task<(Project project, Sprint sprint)> CreateProjectAndSprintAsync(TaskSprintBoardDbContext context, string projectName = "Alpha Project", string sprintName = "Sprint 1")
    {
        var projectRepo = new ProjectRepository(context);
        var sprintRepo = new SprintRepository(context);

        var project = await projectRepo.AddAsync(new Project
        {
            Name = projectName,
            Description = "Project description",
            Status = "Active",
            CreatedBy = null,
            IsDeleted = false,
            IsActive = true
        });

        var sprint = await sprintRepo.AddAsync(new Sprint
        {
            ProjectId = project.Id,
            Name = sprintName,
            Goal = "Sprint goal",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(10),
            Status = "Active",
            CreatedBy = null,
            IsDeleted = false,
            IsActive = true
        });

        return (project, sprint);
    }

    [Fact]
    public async Task ProjectService_CreateProject_ShouldStoreProject()
    {
        using var context = CreateContext();
        var repo = new ProjectRepository(context);
        var service = new ProjectService(repo);

        var result = await service.CreateProjectAsync("Alpha Project", "Description", DateTime.UtcNow, DateTime.UtcNow.AddDays(10), "Active", 1);

        Assert.Equal("Alpha Project", result.Name);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task ProjectService_DuplicateProject_ShouldThrow()
    {
        using var context = CreateContext();
        var repo = new ProjectRepository(context);
        var service = new ProjectService(repo);

        await service.CreateProjectAsync("Alpha Project", "Description", DateTime.UtcNow, DateTime.UtcNow.AddDays(10), "Active", 1);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateProjectAsync("Alpha Project", "Description", DateTime.UtcNow, DateTime.UtcNow.AddDays(10), "Active", 1));
    }

    [Fact]
    public async Task TaskService_GetTasks_ShouldReturnTaskList()
    {
        using var context = CreateContext();
        var projectRepo = new ProjectRepository(context);
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var historyRepo = new StatusHistoryRepository(context);
        var service = new TaskService(taskRepo, historyRepo, sprintRepo);

        var (_, sprint) = await CreateProjectAndSprintAsync(context);
        await taskRepo.AddAsync(new TaskItem
        {
            SprintId = sprint.Id,
            Title = "Task A",
            Description = "Test task",
            Priority = "High",
            Status = "Todo",
            EstimateHours = 8,
            ProgressPercentage = 10,
            CreatedBy = null,
            IsDeleted = false,
            IsActive = true
        });

        var (items, totalCount) = await service.SearchTasksAsync(null, null, null, null, sprint.Id, null, null, null, 1, 10);

        Assert.Equal(1, totalCount);
        Assert.Single(items);
    }

    [Fact]
    public async Task TaskService_GetTaskById_ShouldReturnTask()
    {
        using var context = CreateContext();
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var historyRepo = new StatusHistoryRepository(context);
        var service = new TaskService(taskRepo, historyRepo, sprintRepo);

        var (_, sprint) = await CreateProjectAndSprintAsync(context);
        var task = await taskRepo.AddAsync(new TaskItem
        {
            SprintId = sprint.Id,
            Title = "Task B",
            Description = "Task B description",
            Priority = "Medium",
            Status = "Todo",
            EstimateHours = 5,
            ProgressPercentage = 20,
            CreatedBy = null,
            IsDeleted = false,
            IsActive = true
        });

        var result = await service.GetByIdAsync(task.Id);

        Assert.NotNull(result);
        Assert.Equal("Task B", result!.Title);
    }

    [Fact]
    public async Task TaskService_CreateTask_ShouldStoreTask()
    {
        using var context = CreateContext();
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var historyRepo = new StatusHistoryRepository(context);
        var service = new TaskService(taskRepo, historyRepo, sprintRepo);

        var (_, sprint) = await CreateProjectAndSprintAsync(context);
        var result = await service.CreateTaskAsync(new TaskCreateDto
        {
            SprintId = sprint.Id,
            Title = "New Task",
            Description = "Details",
            Priority = "High",
            Status = "Todo",
            EstimateHours = 12,
            ProgressPercentage = 0,
            StartDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(3)
        }, 1);

        Assert.Equal("New Task", result.Title);
        Assert.Equal(sprint.Id, result.SprintId);
    }

    [Fact]
    public async Task TaskService_UpdateTask_ShouldUpdateTask()
    {
        using var context = CreateContext();
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var historyRepo = new StatusHistoryRepository(context);
        var service = new TaskService(taskRepo, historyRepo, sprintRepo);

        var (_, sprint) = await CreateProjectAndSprintAsync(context);
        var task = await taskRepo.AddAsync(new TaskItem
        {
            SprintId = sprint.Id,
            Title = "Task C",
            Description = "Original",
            Priority = "Low",
            Status = "Todo",
            EstimateHours = 6,
            ProgressPercentage = 10,
            CreatedBy = null,
            IsDeleted = false,
            IsActive = true
        });

        var updated = await service.UpdateTaskAsync(task.Id, new TaskUpdateDto
        {
            Title = "Updated Task C",
            Priority = "High",
            ProgressPercentage = 40
        }, 1);

        Assert.Equal("Updated Task C", updated.Title);
        Assert.Equal("High", updated.Priority);
        Assert.Equal(40, updated.ProgressPercentage);
    }

    [Fact]
    public async Task TaskService_DuplicateTask_ShouldThrow()
    {
        using var context = CreateContext();
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var historyRepo = new StatusHistoryRepository(context);
        var service = new TaskService(taskRepo, historyRepo, sprintRepo);

        var (_, sprint) = await CreateProjectAndSprintAsync(context);
        await service.CreateTaskAsync(new TaskCreateDto
        {
            SprintId = sprint.Id,
            Title = "Duplicate",
            Description = "First",
            Priority = "Medium",
            Status = "Todo",
            EstimateHours = 3,
            ProgressPercentage = 5,
            StartDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(2)
        }, 1);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateTaskAsync(new TaskCreateDto
            {
                SprintId = sprint.Id,
                Title = "Duplicate",
                Description = "Second",
                Priority = "Medium",
                Status = "Todo",
                EstimateHours = 4,
                ProgressPercentage = 10,
                StartDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(3)
            }, 1));
    }

    [Fact]
    public async Task TaskService_InvalidEstimate_ShouldThrow()
    {
        using var context = CreateContext();
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var historyRepo = new StatusHistoryRepository(context);
        var service = new TaskService(taskRepo, historyRepo, sprintRepo);

        var (_, sprint) = await CreateProjectAndSprintAsync(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateTaskAsync(new TaskCreateDto
            {
                SprintId = sprint.Id,
                Title = "Bad estimate",
                Description = "Bad",
                Priority = "Medium",
                Status = "Todo",
                EstimateHours = 0,
                ProgressPercentage = 0,
                StartDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(1)
            }, 1));
    }

    [Fact]
    public async Task TaskService_InvalidProgress_ShouldThrow()
    {
        using var context = CreateContext();
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var historyRepo = new StatusHistoryRepository(context);
        var service = new TaskService(taskRepo, historyRepo, sprintRepo);

        var (_, sprint) = await CreateProjectAndSprintAsync(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateTaskAsync(new TaskCreateDto
            {
                SprintId = sprint.Id,
                Title = "Bad progress",
                Description = "Bad",
                Priority = "Medium",
                Status = "Todo",
                EstimateHours = 8,
                ProgressPercentage = 101,
                StartDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(1)
            }, 1));
    }

    [Fact]
    public async Task TaskService_InvalidSprintId_ShouldThrow()
    {
        using var context = CreateContext();
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var historyRepo = new StatusHistoryRepository(context);
        var service = new TaskService(taskRepo, historyRepo, sprintRepo);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateTaskAsync(new TaskCreateDto
            {
                SprintId = int.MaxValue,
                Title = "Bad sprint",
                Description = "Bad",
                Priority = "Medium",
                Status = "Todo",
                EstimateHours = 8,
                ProgressPercentage = 10,
                StartDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(1)
            }, 1));
    }

    [Fact]
    public async Task TaskService_DeleteTask_ShouldSoftDelete()
    {
        using var context = CreateContext();
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var historyRepo = new StatusHistoryRepository(context);
        var service = new TaskService(taskRepo, historyRepo, sprintRepo);

        var (_, sprint) = await CreateProjectAndSprintAsync(context);
        var task = await taskRepo.AddAsync(new TaskItem
        {
            SprintId = sprint.Id,
            Title = "Delete me",
            Description = "Delete",
            Priority = "Medium",
            Status = "Todo",
            EstimateHours = 5,
            ProgressPercentage = 1,
            CreatedBy = null,
            IsDeleted = false,
            IsActive = true
        });

        await service.DeleteTaskAsync(task.Id, 1);
        var deleted = await service.GetByIdAsync(task.Id);

        Assert.Null(deleted);
    }

    [Fact]
    public async Task TaskService_InvalidStatusTransition_ShouldThrow()
    {
        using var context = CreateContext();
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var historyRepo = new StatusHistoryRepository(context);
        var service = new TaskService(taskRepo, historyRepo, sprintRepo);

        var (_, sprint) = await CreateProjectAndSprintAsync(context);
        var task = await taskRepo.AddAsync(new TaskItem
        {
            SprintId = sprint.Id,
            Title = "Task A",
            Description = "Test task",
            Priority = "High",
            Status = "Todo",
            EstimateHours = 8,
            ProgressPercentage = 10,
            CreatedBy = null,
            IsDeleted = false,
            IsActive = true
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateTaskAsync(task.Id, new TaskUpdateDto { Status = "Blocked" }, 1));
    }

    [Fact]
    public async Task AssignmentService_GetAssignments_ShouldReturnAssignmentList()
    {
        using var context = CreateContext();
        var projectRepo = new ProjectRepository(context);
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var userRepo = new UserRepository(context);
        var assignmentRepo = new AssignmentRepository(context);
        var service = new AssignmentService(assignmentRepo, taskRepo, userRepo);

        var project = await projectRepo.AddAsync(new Project { Name = "Project A", Description = "Desc", Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var sprint = await sprintRepo.AddAsync(new Sprint { ProjectId = project.Id, Name = "Sprint A", Goal = "Goal", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(5), Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var task = await taskRepo.AddAsync(new TaskItem { SprintId = sprint.Id, Title = "Task A", Description = "Details", Priority = "High", Status = "Todo", EstimateHours = 8, ProgressPercentage = 0, CreatedBy = null, IsDeleted = false, IsActive = true });
        var user = await userRepo.AddAsync(new User { FirstName = "Alice", LastName = "Test", Email = "alice@example.com", UserName = "alice_test", PasswordHash = "hash", Role = "TeamMember", CreatedBy = null, IsDeleted = false, IsActive = true });
        await assignmentRepo.AddAsync(new Assignment { TaskId = task.Id, UserId = user.Id, AssignedAt = DateTime.UtcNow, RoleInTask = "Owner", CreatedBy = null, IsDeleted = false, IsActive = true });

        var (items, totalCount) = await service.SearchAssignmentsAsync(null, task.Id, null, null, 1, 10);

        Assert.Equal(1, totalCount);
        Assert.Single(items);
    }

    [Fact]
    public async Task AssignmentService_GetAssignmentById_ShouldReturnAssignment()
    {
        using var context = CreateContext();
        var projectRepo = new ProjectRepository(context);
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var userRepo = new UserRepository(context);
        var assignmentRepo = new AssignmentRepository(context);
        var service = new AssignmentService(assignmentRepo, taskRepo, userRepo);

        var project = await projectRepo.AddAsync(new Project { Name = "Project B", Description = "Desc", Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var sprint = await sprintRepo.AddAsync(new Sprint { ProjectId = project.Id, Name = "Sprint B", Goal = "Goal", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(5), Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var task = await taskRepo.AddAsync(new TaskItem { SprintId = sprint.Id, Title = "Task B", Description = "Details", Priority = "Medium", Status = "Todo", EstimateHours = 4, ProgressPercentage = 0, CreatedBy = null, IsDeleted = false, IsActive = true });
        var user = await userRepo.AddAsync(new User { FirstName = "Bob", LastName = "Test", Email = "bob@example.com", UserName = "bob_test", PasswordHash = "hash", Role = "TeamMember", CreatedBy = null, IsDeleted = false, IsActive = true });
        var assignment = await assignmentRepo.AddAsync(new Assignment { TaskId = task.Id, UserId = user.Id, AssignedAt = DateTime.UtcNow, RoleInTask = "Contributor", CreatedBy = null, IsDeleted = false, IsActive = true });

        var result = await service.GetByIdAsync(assignment.Id);

        Assert.NotNull(result);
        Assert.Equal(task.Id, result!.TaskId);
    }

    [Fact]
    public async Task AssignmentService_CreateAssignment_ShouldStoreAssignment()
    {
        using var context = CreateContext();
        var projectRepo = new ProjectRepository(context);
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var userRepo = new UserRepository(context);
        var assignmentRepo = new AssignmentRepository(context);
        var service = new AssignmentService(assignmentRepo, taskRepo, userRepo);

        var project = await projectRepo.AddAsync(new Project { Name = "Project C", Description = "Desc", Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var sprint = await sprintRepo.AddAsync(new Sprint { ProjectId = project.Id, Name = "Sprint C", Goal = "Goal", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(7), Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var task = await taskRepo.AddAsync(new TaskItem { SprintId = sprint.Id, Title = "Task C", Description = "Details", Priority = "Low", Status = "Todo", EstimateHours = 7, ProgressPercentage = 0, CreatedBy = null, IsDeleted = false, IsActive = true });
        var user = await userRepo.AddAsync(new User { FirstName = "Cara", LastName = "Test", Email = "cara@example.com", UserName = "cara_test", PasswordHash = "hash", Role = "TeamMember", CreatedBy = null, IsDeleted = false, IsActive = true });

        var result = await service.CreateAssignmentAsync(new AssignmentCreateDto { TaskId = task.Id, UserId = user.Id, RoleInTask = "Owner" }, 1);

        Assert.Equal(task.Id, result.TaskId);
        Assert.Equal(user.Id, result.UserId);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task AssignmentService_UpdateAssignment_ShouldUpdateAssignment()
    {
        using var context = CreateContext();
        var projectRepo = new ProjectRepository(context);
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var userRepo = new UserRepository(context);
        var assignmentRepo = new AssignmentRepository(context);
        var service = new AssignmentService(assignmentRepo, taskRepo, userRepo);

        var project = await projectRepo.AddAsync(new Project { Name = "Project D", Description = "Desc", Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var sprint = await sprintRepo.AddAsync(new Sprint { ProjectId = project.Id, Name = "Sprint D", Goal = "Goal", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(8), Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var task = await taskRepo.AddAsync(new TaskItem { SprintId = sprint.Id, Title = "Task D", Description = "Details", Priority = "High", Status = "Todo", EstimateHours = 9, ProgressPercentage = 0, CreatedBy = null, IsDeleted = false, IsActive = true });
        var user = await userRepo.AddAsync(new User { FirstName = "Diana", LastName = "Test", Email = "diana@example.com", UserName = "diana_test", PasswordHash = "hash", Role = "TeamMember", CreatedBy = null, IsDeleted = false, IsActive = true });
        var assignment = await assignmentRepo.AddAsync(new Assignment { TaskId = task.Id, UserId = user.Id, AssignedAt = DateTime.UtcNow, RoleInTask = "Owner", CreatedBy = null, IsDeleted = false, IsActive = true });

        var updated = await service.UpdateAssignmentAsync(assignment.Id, new AssignmentUpdateDto { RoleInTask = "Reviewer", IsActive = false }, 1);

        Assert.Equal("Reviewer", updated.RoleInTask);
        Assert.False(updated.IsActive);
        Assert.NotNull(updated.UnassignedAt);
    }

    [Fact]
    public async Task AssignmentService_DeleteAssignment_ShouldSoftDelete()
    {
        using var context = CreateContext();
        var projectRepo = new ProjectRepository(context);
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var userRepo = new UserRepository(context);
        var assignmentRepo = new AssignmentRepository(context);
        var service = new AssignmentService(assignmentRepo, taskRepo, userRepo);

        var project = await projectRepo.AddAsync(new Project { Name = "Project E", Description = "Desc", Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var sprint = await sprintRepo.AddAsync(new Sprint { ProjectId = project.Id, Name = "Sprint E", Goal = "Goal", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(9), Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var task = await taskRepo.AddAsync(new TaskItem { SprintId = sprint.Id, Title = "Task E", Description = "Details", Priority = "Medium", Status = "Todo", EstimateHours = 6, ProgressPercentage = 0, CreatedBy = null, IsDeleted = false, IsActive = true });
        var user = await userRepo.AddAsync(new User { FirstName = "Evan", LastName = "Test", Email = "evan@example.com", UserName = "evan_test", PasswordHash = "hash", Role = "TeamMember", CreatedBy = null, IsDeleted = false, IsActive = true });
        var assignment = await assignmentRepo.AddAsync(new Assignment { TaskId = task.Id, UserId = user.Id, AssignedAt = DateTime.UtcNow, RoleInTask = "Owner", CreatedBy = null, IsDeleted = false, IsActive = true });

        await service.DeleteAssignmentAsync(assignment.Id, 1);
        var result = await service.GetByIdAsync(assignment.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task AssignmentService_InvalidTaskId_ShouldThrow()
    {
        using var context = CreateContext();
        var taskRepo = new TaskRepository(context);
        var userRepo = new UserRepository(context);
        var assignmentRepo = new AssignmentRepository(context);
        var service = new AssignmentService(assignmentRepo, taskRepo, userRepo);

        var user = await userRepo.AddAsync(new User { FirstName = "Frank", LastName = "Test", Email = "frank@example.com", UserName = "frank_test", PasswordHash = "hash", Role = "TeamMember", CreatedBy = null, IsDeleted = false, IsActive = true });

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAssignmentAsync(new AssignmentCreateDto { TaskId = int.MaxValue, UserId = user.Id, RoleInTask = "Owner" }, 1));
    }

    [Fact]
    public async Task AssignmentService_InvalidUserId_ShouldThrow()
    {
        using var context = CreateContext();
        var projectRepo = new ProjectRepository(context);
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var userRepo = new UserRepository(context);
        var assignmentRepo = new AssignmentRepository(context);
        var service = new AssignmentService(assignmentRepo, taskRepo, userRepo);

        var project = await projectRepo.AddAsync(new Project { Name = "Project F", Description = "Desc", Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var sprint = await sprintRepo.AddAsync(new Sprint { ProjectId = project.Id, Name = "Sprint F", Goal = "Goal", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(10), Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var task = await taskRepo.AddAsync(new TaskItem { SprintId = sprint.Id, Title = "Task F", Description = "Details", Priority = "Medium", Status = "Todo", EstimateHours = 5, ProgressPercentage = 0, CreatedBy = null, IsDeleted = false, IsActive = true });

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateAssignmentAsync(new AssignmentCreateDto { TaskId = task.Id, UserId = int.MaxValue, RoleInTask = "Owner" }, 1));
    }

    [Fact]
    public async Task AssignmentService_DuplicateActiveAssignment_ShouldThrow()
    {
        using var context = CreateContext();
        var projectRepo = new ProjectRepository(context);
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var userRepo = new UserRepository(context);
        var assignmentRepo = new AssignmentRepository(context);
        var service = new AssignmentService(assignmentRepo, taskRepo, userRepo);

        var project = await projectRepo.AddAsync(new Project { Name = "Project G", Description = "Desc", Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var sprint = await sprintRepo.AddAsync(new Sprint { ProjectId = project.Id, Name = "Sprint G", Goal = "Goal", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(11), Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var task = await taskRepo.AddAsync(new TaskItem { SprintId = sprint.Id, Title = "Task G", Description = "Details", Priority = "High", Status = "Todo", EstimateHours = 8, ProgressPercentage = 0, CreatedBy = null, IsDeleted = false, IsActive = true });
        var user = await userRepo.AddAsync(new User { FirstName = "Grace", LastName = "Test", Email = "grace@example.com", UserName = "grace_test", PasswordHash = "hash", Role = "TeamMember", CreatedBy = null, IsDeleted = false, IsActive = true });

        await service.CreateAssignmentAsync(new AssignmentCreateDto { TaskId = task.Id, UserId = user.Id, RoleInTask = "Owner" }, 1);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAssignmentAsync(new AssignmentCreateDto { TaskId = task.Id, UserId = user.Id, RoleInTask = "Contributor" }, 1));
    }

    [Fact]
    public async Task AssignmentService_DeletedAssignmentExcludedFromNormalQueries()
    {
        using var context = CreateContext();
        var projectRepo = new ProjectRepository(context);
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var userRepo = new UserRepository(context);
        var assignmentRepo = new AssignmentRepository(context);
        var service = new AssignmentService(assignmentRepo, taskRepo, userRepo);

        var project = await projectRepo.AddAsync(new Project { Name = "Project H", Description = "Desc", Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var sprint = await sprintRepo.AddAsync(new Sprint { ProjectId = project.Id, Name = "Sprint H", Goal = "Goal", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(12), Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var task = await taskRepo.AddAsync(new TaskItem { SprintId = sprint.Id, Title = "Task H", Description = "Details", Priority = "High", Status = "Todo", EstimateHours = 10, ProgressPercentage = 0, CreatedBy = null, IsDeleted = false, IsActive = true });
        var user = await userRepo.AddAsync(new User { FirstName = "Hank", LastName = "Test", Email = "hank@example.com", UserName = "hank_test", PasswordHash = "hash", Role = "TeamMember", CreatedBy = null, IsDeleted = false, IsActive = true });
        var assignment = await assignmentRepo.AddAsync(new Assignment { TaskId = task.Id, UserId = user.Id, AssignedAt = DateTime.UtcNow, RoleInTask = "Owner", CreatedBy = null, IsDeleted = false, IsActive = true });

        await service.DeleteAssignmentAsync(assignment.Id, 1);
        var (items, totalCount) = await service.SearchAssignmentsAsync(null, task.Id, null, null, 1, 10);

        Assert.Equal(0, totalCount);
        Assert.Empty(items);
    }

    [Fact]
    public async Task CommentService_GetComments_ShouldReturnCommentList()
    {
        using var context = CreateContext();
        var projectRepo = new ProjectRepository(context);
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var userRepo = new UserRepository(context);
        var commentRepo = new CommentRepository(context);
        var service = new CommentService(commentRepo, taskRepo, userRepo);

        var project = await projectRepo.AddAsync(new Project { Name = "Comment Project", Description = "Desc", Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var sprint = await sprintRepo.AddAsync(new Sprint { ProjectId = project.Id, Name = "Comment Sprint", Goal = "Goal", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(5), Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var task = await taskRepo.AddAsync(new TaskItem { SprintId = sprint.Id, Title = "Comment Task", Description = "Details", Priority = "High", Status = "Todo", EstimateHours = 8, ProgressPercentage = 0, CreatedBy = null, IsDeleted = false, IsActive = true });
        var user = await userRepo.AddAsync(new User { FirstName = "Eve", LastName = "Test", Email = "eve@example.com", UserName = "eve_test", PasswordHash = "hash", Role = "TeamMember", CreatedBy = null, IsDeleted = false, IsActive = true });
        await commentRepo.AddAsync(new Comment { TaskId = task.Id, UserId = user.Id, Message = "Looks good.", CreatedBy = null, IsDeleted = false, IsActive = true });

        var (items, totalCount) = await service.SearchCommentsAsync(null, task.Id, null, null, 1, 10);

        Assert.Equal(1, totalCount);
        Assert.Single(items);
    }

    [Fact]
    public async Task CommentService_GetCommentById_ShouldReturnComment()
    {
        using var context = CreateContext();
        var projectRepo = new ProjectRepository(context);
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var userRepo = new UserRepository(context);
        var commentRepo = new CommentRepository(context);
        var service = new CommentService(commentRepo, taskRepo, userRepo);

        var project = await projectRepo.AddAsync(new Project { Name = "Comment Project 2", Description = "Desc", Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var sprint = await sprintRepo.AddAsync(new Sprint { ProjectId = project.Id, Name = "Comment Sprint 2", Goal = "Goal", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(5), Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var task = await taskRepo.AddAsync(new TaskItem { SprintId = sprint.Id, Title = "Comment Task 2", Description = "Details", Priority = "Medium", Status = "Todo", EstimateHours = 4, ProgressPercentage = 0, CreatedBy = null, IsDeleted = false, IsActive = true });
        var user = await userRepo.AddAsync(new User { FirstName = "Frank", LastName = "Test", Email = "frank@example.com", UserName = "frank_test", PasswordHash = "hash", Role = "TeamMember", CreatedBy = null, IsDeleted = false, IsActive = true });
        var comment = await commentRepo.AddAsync(new Comment { TaskId = task.Id, UserId = user.Id, Message = "Needs review.", CreatedBy = null, IsDeleted = false, IsActive = true });

        var result = await service.GetByIdAsync(comment.Id);

        Assert.NotNull(result);
        Assert.Equal("Needs review.", result!.Message);
    }

    [Fact]
    public async Task CommentService_CreateComment_ShouldStoreComment()
    {
        using var context = CreateContext();
        var projectRepo = new ProjectRepository(context);
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var userRepo = new UserRepository(context);
        var commentRepo = new CommentRepository(context);
        var service = new CommentService(commentRepo, taskRepo, userRepo);

        var project = await projectRepo.AddAsync(new Project { Name = "Comment Project 3", Description = "Desc", Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var sprint = await sprintRepo.AddAsync(new Sprint { ProjectId = project.Id, Name = "Comment Sprint 3", Goal = "Goal", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(5), Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var task = await taskRepo.AddAsync(new TaskItem { SprintId = sprint.Id, Title = "Comment Task 3", Description = "Details", Priority = "Low", Status = "Todo", EstimateHours = 2, ProgressPercentage = 0, CreatedBy = null, IsDeleted = false, IsActive = true });
        var user = await userRepo.AddAsync(new User { FirstName = "Grace", LastName = "Test", Email = "grace@example.com", UserName = "grace_test", PasswordHash = "hash", Role = "TeamMember", CreatedBy = null, IsDeleted = false, IsActive = true });

        var result = await service.CreateCommentAsync(new CommentCreateDto { TaskId = task.Id, UserId = user.Id, Message = "Added note" }, 1);

        Assert.Equal(task.Id, result.TaskId);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal("Added note", result.Message);
    }

    [Fact]
    public async Task CommentService_UpdateComment_ShouldUpdateComment()
    {
        using var context = CreateContext();
        var projectRepo = new ProjectRepository(context);
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var userRepo = new UserRepository(context);
        var commentRepo = new CommentRepository(context);
        var service = new CommentService(commentRepo, taskRepo, userRepo);

        var project = await projectRepo.AddAsync(new Project { Name = "Comment Project 4", Description = "Desc", Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var sprint = await sprintRepo.AddAsync(new Sprint { ProjectId = project.Id, Name = "Comment Sprint 4", Goal = "Goal", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(5), Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var task = await taskRepo.AddAsync(new TaskItem { SprintId = sprint.Id, Title = "Comment Task 4", Description = "Details", Priority = "High", Status = "Todo", EstimateHours = 6, ProgressPercentage = 0, CreatedBy = null, IsDeleted = false, IsActive = true });
        var user = await userRepo.AddAsync(new User { FirstName = "Hank", LastName = "Test", Email = "hank@example.com", UserName = "hank_test", PasswordHash = "hash", Role = "TeamMember", CreatedBy = null, IsDeleted = false, IsActive = true });
        var comment = await commentRepo.AddAsync(new Comment { TaskId = task.Id, UserId = user.Id, Message = "Original note", CreatedBy = null, IsDeleted = false, IsActive = true });

        var updated = await service.UpdateCommentAsync(comment.Id, new CommentUpdateDto { Message = "Updated note" }, 1);

        Assert.Equal("Updated note", updated.Message);
    }

    [Fact]
    public async Task CommentService_DeleteComment_ShouldSoftDelete()
    {
        using var context = CreateContext();
        var projectRepo = new ProjectRepository(context);
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var userRepo = new UserRepository(context);
        var commentRepo = new CommentRepository(context);
        var service = new CommentService(commentRepo, taskRepo, userRepo);

        var project = await projectRepo.AddAsync(new Project { Name = "Comment Project 5", Description = "Desc", Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var sprint = await sprintRepo.AddAsync(new Sprint { ProjectId = project.Id, Name = "Comment Sprint 5", Goal = "Goal", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(5), Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var task = await taskRepo.AddAsync(new TaskItem { SprintId = sprint.Id, Title = "Comment Task 5", Description = "Details", Priority = "High", Status = "Todo", EstimateHours = 3, ProgressPercentage = 0, CreatedBy = null, IsDeleted = false, IsActive = true });
        var user = await userRepo.AddAsync(new User { FirstName = "Ivy", LastName = "Test", Email = "ivy@example.com", UserName = "ivy_test", PasswordHash = "hash", Role = "TeamMember", CreatedBy = null, IsDeleted = false, IsActive = true });
        var comment = await commentRepo.AddAsync(new Comment { TaskId = task.Id, UserId = user.Id, Message = "Delete me", CreatedBy = null, IsDeleted = false, IsActive = true });

        await service.DeleteCommentAsync(comment.Id, 1);

        var result = await service.GetByIdAsync(comment.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task CommentService_InvalidTaskId_ShouldThrow()
    {
        using var context = CreateContext();
        var taskRepo = new TaskRepository(context);
        var userRepo = new UserRepository(context);
        var commentRepo = new CommentRepository(context);
        var service = new CommentService(commentRepo, taskRepo, userRepo);

        var user = await userRepo.AddAsync(new User { FirstName = "Jill", LastName = "Test", Email = "jill@example.com", UserName = "jill_test", PasswordHash = "hash", Role = "TeamMember", CreatedBy = null, IsDeleted = false, IsActive = true });

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateCommentAsync(new CommentCreateDto { TaskId = int.MaxValue, UserId = user.Id, Message = "Note" }, 1));
    }

    [Fact]
    public async Task CommentService_InvalidUserId_ShouldThrow()
    {
        using var context = CreateContext();
        var projectRepo = new ProjectRepository(context);
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var userRepo = new UserRepository(context);
        var commentRepo = new CommentRepository(context);
        var service = new CommentService(commentRepo, taskRepo, userRepo);

        var project = await projectRepo.AddAsync(new Project { Name = "Comment Project 6", Description = "Desc", Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var sprint = await sprintRepo.AddAsync(new Sprint { ProjectId = project.Id, Name = "Comment Sprint 6", Goal = "Goal", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(5), Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var task = await taskRepo.AddAsync(new TaskItem { SprintId = sprint.Id, Title = "Comment Task 6", Description = "Details", Priority = "Medium", Status = "Todo", EstimateHours = 5, ProgressPercentage = 0, CreatedBy = null, IsDeleted = false, IsActive = true });

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateCommentAsync(new CommentCreateDto { TaskId = task.Id, UserId = int.MaxValue, Message = "Note" }, 1));
    }

    [Fact]
    public async Task CommentService_EmptyMessage_ShouldThrow()
    {
        using var context = CreateContext();
        var projectRepo = new ProjectRepository(context);
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var userRepo = new UserRepository(context);
        var commentRepo = new CommentRepository(context);
        var service = new CommentService(commentRepo, taskRepo, userRepo);

        var project = await projectRepo.AddAsync(new Project { Name = "Comment Project 7", Description = "Desc", Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var sprint = await sprintRepo.AddAsync(new Sprint { ProjectId = project.Id, Name = "Comment Sprint 7", Goal = "Goal", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(5), Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var task = await taskRepo.AddAsync(new TaskItem { SprintId = sprint.Id, Title = "Comment Task 7", Description = "Details", Priority = "Low", Status = "Todo", EstimateHours = 2, ProgressPercentage = 0, CreatedBy = null, IsDeleted = false, IsActive = true });
        var user = await userRepo.AddAsync(new User { FirstName = "Kai", LastName = "Test", Email = "kai@example.com", UserName = "kai_test", PasswordHash = "hash", Role = "TeamMember", CreatedBy = null, IsDeleted = false, IsActive = true });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateCommentAsync(new CommentCreateDto { TaskId = task.Id, UserId = user.Id, Message = "   " }, 1));
    }

    [Fact]
    public async Task CommentService_TooLongMessage_ShouldThrow()
    {
        using var context = CreateContext();
        var projectRepo = new ProjectRepository(context);
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var userRepo = new UserRepository(context);
        var commentRepo = new CommentRepository(context);
        var service = new CommentService(commentRepo, taskRepo, userRepo);

        var project = await projectRepo.AddAsync(new Project { Name = "Comment Project 8", Description = "Desc", Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var sprint = await sprintRepo.AddAsync(new Sprint { ProjectId = project.Id, Name = "Comment Sprint 8", Goal = "Goal", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(5), Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var task = await taskRepo.AddAsync(new TaskItem { SprintId = sprint.Id, Title = "Comment Task 8", Description = "Details", Priority = "Medium", Status = "Todo", EstimateHours = 4, ProgressPercentage = 0, CreatedBy = null, IsDeleted = false, IsActive = true });
        var user = await userRepo.AddAsync(new User { FirstName = "Leo", LastName = "Test", Email = "leo@example.com", UserName = "leo_test", PasswordHash = "hash", Role = "TeamMember", CreatedBy = null, IsDeleted = false, IsActive = true });

        var longMessage = new string('x', 2001);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateCommentAsync(new CommentCreateDto { TaskId = task.Id, UserId = user.Id, Message = longMessage }, 1));
    }

    [Fact]
    public async Task CommentService_DeletedCommentExcludedFromNormalQueries()
    {
        using var context = CreateContext();
        var projectRepo = new ProjectRepository(context);
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var userRepo = new UserRepository(context);
        var commentRepo = new CommentRepository(context);
        var service = new CommentService(commentRepo, taskRepo, userRepo);

        var project = await projectRepo.AddAsync(new Project { Name = "Comment Project 9", Description = "Desc", Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var sprint = await sprintRepo.AddAsync(new Sprint { ProjectId = project.Id, Name = "Comment Sprint 9", Goal = "Goal", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(5), Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var task = await taskRepo.AddAsync(new TaskItem { SprintId = sprint.Id, Title = "Comment Task 9", Description = "Details", Priority = "High", Status = "Todo", EstimateHours = 7, ProgressPercentage = 0, CreatedBy = null, IsDeleted = false, IsActive = true });
        var user = await userRepo.AddAsync(new User { FirstName = "Mia", LastName = "Test", Email = "mia@example.com", UserName = "mia_test", PasswordHash = "hash", Role = "TeamMember", CreatedBy = null, IsDeleted = false, IsActive = true });
        var comment = await commentRepo.AddAsync(new Comment { TaskId = task.Id, UserId = user.Id, Message = "Should be hidden", CreatedBy = null, IsDeleted = false, IsActive = true });

        await service.DeleteCommentAsync(comment.Id, 1);
        var (items, totalCount) = await service.SearchCommentsAsync(null, task.Id, null, null, 1, 10);

        Assert.Equal(0, totalCount);
        Assert.Empty(items);
    }

    [Fact]
    public async Task TaskService_StatusChange_ShouldCreateStatusHistory()
    {
        using var context = CreateContext();
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var historyRepo = new StatusHistoryRepository(context);
        var service = new TaskService(taskRepo, historyRepo, sprintRepo);

        var (_, sprint) = await CreateProjectAndSprintAsync(context);
        var task = await taskRepo.AddAsync(new TaskItem
        {
            SprintId = sprint.Id,
            Title = "Status Task",
            Description = "Task for status history",
            Priority = "High",
            Status = "Todo",
            EstimateHours = 5,
            ProgressPercentage = 10,
            CreatedBy = null,
            IsDeleted = false,
            IsActive = true
        });

        await service.UpdateTaskAsync(task.Id, new TaskUpdateDto { Status = "InProgress" }, 1);

        var history = await historyRepo.GetByTaskIdAsync(task.Id);
        var latest = history.First();

        Assert.Single(history);
        Assert.Equal("Todo", latest.OldStatus);
        Assert.Equal("InProgress", latest.NewStatus);
    }

    [Fact]
    public async Task TaskService_UnchangedStatus_ShouldNotCreateStatusHistory()
    {
        using var context = CreateContext();
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var historyRepo = new StatusHistoryRepository(context);
        var service = new TaskService(taskRepo, historyRepo, sprintRepo);

        var (_, sprint) = await CreateProjectAndSprintAsync(context);
        var task = await taskRepo.AddAsync(new TaskItem
        {
            SprintId = sprint.Id,
            Title = "Status Task 2",
            Description = "Task for unchanged status",
            Priority = "Medium",
            Status = "Todo",
            EstimateHours = 4,
            ProgressPercentage = 15,
            CreatedBy = null,
            IsDeleted = false,
            IsActive = true
        });

        await service.UpdateTaskAsync(task.Id, new TaskUpdateDto { Description = "updated description" }, 1);

        var history = await historyRepo.GetByTaskIdAsync(task.Id);
        Assert.Empty(history);
    }

    [Fact]
    public async Task TaskService_InvalidStatus_ShouldThrow()
    {
        using var context = CreateContext();
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var historyRepo = new StatusHistoryRepository(context);
        var service = new TaskService(taskRepo, historyRepo, sprintRepo);

        var (_, sprint) = await CreateProjectAndSprintAsync(context);
        var task = await taskRepo.AddAsync(new TaskItem
        {
            SprintId = sprint.Id,
            Title = "Bad Status Task",
            Description = "Intro",
            Priority = "High",
            Status = "Todo",
            EstimateHours = 2,
            ProgressPercentage = 5,
            CreatedBy = null,
            IsDeleted = false,
            IsActive = true
        });

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateTaskAsync(task.Id, new TaskUpdateDto { Status = "NotARealStatus" }, 1));
    }

    [Fact]
    public async Task StatusHistoryService_GetByTaskId_ShouldReturnHistory()
    {
        using var context = CreateContext();
        var projectRepo = new ProjectRepository(context);
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var statusHistoryRepo = new StatusHistoryRepository(context);
        var service = new StatusHistoryService(statusHistoryRepo, taskRepo);

        var project = await projectRepo.AddAsync(new Project { Name = "History Project", Description = "Desc", Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var sprint = await sprintRepo.AddAsync(new Sprint { ProjectId = project.Id, Name = "History Sprint", Goal = "Goal", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(8), Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var task = await taskRepo.AddAsync(new TaskItem { SprintId = sprint.Id, Title = "History Task", Description = "Description", Priority = "High", Status = "Todo", EstimateHours = 6, ProgressPercentage = 0, CreatedBy = null, IsDeleted = false, IsActive = true });

        await statusHistoryRepo.AddAsync(new StatusHistory { TaskId = task.Id, OldStatus = "Todo", NewStatus = "InProgress", ChangedBy = "tester", ChangedAt = DateTime.UtcNow, CreatedBy = null, IsDeleted = false, IsActive = true });

        var history = await service.GetByTaskIdAsync(task.Id);

        Assert.Single(history);
        Assert.Equal("InProgress", history.First().NewStatus);
    }

    [Fact]
    public async Task StatusHistoryService_InvalidTaskId_ShouldThrow()
    {
        using var context = CreateContext();
        var statusHistoryRepo = new StatusHistoryRepository(context);
        var taskRepo = new TaskRepository(context);
        var service = new StatusHistoryService(statusHistoryRepo, taskRepo);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateStatusHistoryAsync(0, "Todo", "InProgress", "tester"));
    }

    [Fact]
    public async Task StatusHistoryService_DeletedHistoryExcludedFromNormalQueries()
    {
        using var context = CreateContext();
        var projectRepo = new ProjectRepository(context);
        var sprintRepo = new SprintRepository(context);
        var taskRepo = new TaskRepository(context);
        var statusHistoryRepo = new StatusHistoryRepository(context);
        var service = new StatusHistoryService(statusHistoryRepo, taskRepo);

        var project = await projectRepo.AddAsync(new Project { Name = "Delete History Project", Description = "Desc", Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var sprint = await sprintRepo.AddAsync(new Sprint { ProjectId = project.Id, Name = "Delete History Sprint", Goal = "Goal", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(8), Status = "Active", CreatedBy = null, IsDeleted = false, IsActive = true });
        var task = await taskRepo.AddAsync(new TaskItem { SprintId = sprint.Id, Title = "Delete History Task", Description = "Description", Priority = "High", Status = "Todo", EstimateHours = 6, ProgressPercentage = 0, CreatedBy = null, IsDeleted = false, IsActive = true });
        var history = await statusHistoryRepo.AddAsync(new StatusHistory { TaskId = task.Id, OldStatus = "Todo", NewStatus = "InProgress", ChangedBy = "tester", ChangedAt = DateTime.UtcNow, CreatedBy = null, IsDeleted = false, IsActive = true });

        history.IsDeleted = true;
        history.UpdatedAt = DateTime.UtcNow;
        await statusHistoryRepo.UpdateAsync(history);

        var (items, totalCount) = await service.SearchAsync(task.Id, null, null, 1, 10);

        Assert.Equal(0, totalCount);
        Assert.Empty(items);
    }

    [Fact]
    public async Task AuthService_LoginAsync_ShouldReturnJwtForValidCredentials()
    {
        using var context = CreateContext();
        var userRepo = new UserRepository(context);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "ThisIsAValidJwtKeyForTesting123!",
                ["Jwt:Issuer"] = "TaskSprintBoard",
                ["Jwt:Audience"] = "TaskSprintBoardClients",
                ["Jwt:ExpirationMinutes"] = "60"
            })
            .Build();
        var service = new AuthService(userRepo, config);

        var plainPassword = "Admin123!";
        var user = await userRepo.AddAsync(new User
        {
            FirstName = "Alice",
            LastName = "Tester",
            Email = "alice.auth@test.com",
            UserName = "alice_auth",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword),
            Role = "Admin",
            CreatedBy = null,
            IsDeleted = false,
            IsActive = true
        });

        var login = await service.LoginAsync(user.Email, plainPassword);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(login.Token);

        Assert.Equal(user.Id, login.UserId);
        Assert.Equal("Admin", login.Role);
        Assert.Contains(token.Claims, c => c.Type == "role" && c.Value == "Admin");
    }

    [Fact]
    public async Task AuthService_LoginAsync_ShouldRejectInvalidPassword()
    {
        using var context = CreateContext();
        var userRepo = new UserRepository(context);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "ThisIsAValidJwtKeyForTesting123!",
                ["Jwt:Issuer"] = "TaskSprintBoard",
                ["Jwt:Audience"] = "TaskSprintBoardClients",
                ["Jwt:ExpirationMinutes"] = "60"
            })
            .Build();
        var service = new AuthService(userRepo, config);

        await userRepo.AddAsync(new User
        {
            FirstName = "Bob",
            LastName = "Tester",
            Email = "bob.auth@test.com",
            UserName = "bob_auth",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("ValidPass123!"),
            Role = "TeamMember",
            CreatedBy = null,
            IsDeleted = false,
            IsActive = true
        });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync("bob.auth@test.com", "WrongPassword!"));
    }

    [Fact]
    public async Task AuthService_LoginAsync_ShouldNotExposePasswordHash()
    {
        using var context = CreateContext();
        var userRepo = new UserRepository(context);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "ThisIsAValidJwtKeyForTesting123!",
                ["Jwt:Issuer"] = "TaskSprintBoard",
                ["Jwt:Audience"] = "TaskSprintBoardClients",
                ["Jwt:ExpirationMinutes"] = "60"
            })
            .Build();
        var service = new AuthService(userRepo, config);

        var password = "StrongPass123!";
        var user = await userRepo.AddAsync(new User
        {
            FirstName = "Cara",
            LastName = "Tester",
            Email = "cara.auth@test.com",
            UserName = "cara_auth",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "ProjectManager",
            CreatedBy = null,
            IsDeleted = false,
            IsActive = true
        });

        var login = await service.LoginAsync(user.Email, password);
        Assert.DoesNotContain("$2a", login.Token);
        Assert.DoesNotContain(user.PasswordHash, login.Token);
    }

    [Fact]
    public async Task AiRiskService_Evaluate_ShouldReturnRiskLevel()
    {
        using var context = CreateContext();
        var taskRepo = new TaskRepository(context);
        var riskRepo = new TaskRiskEvaluationRepository(context);
        var service = new AiRiskService(riskRepo, taskRepo);

        var task = new TaskItem
        {
            SprintId = int.MaxValue,
            Title = "Risk task",
            Description = "Risky task",
            Priority = "Critical",
            Status = "Blocked",
            EstimateHours = 40,
            ProgressPercentage = 10,
            CreatedBy = null,
            IsDeleted = false,
            IsActive = true
        };

        await taskRepo.AddAsync(task);

        var result = await service.EvaluateAsync(new AiRiskEvaluationInputDto
        {
            TaskId = task.Id,
            EstimateHours = 40,
            TaskAge = 5,
            DaysRemaining = 2,
            ProgressPercentage = 10,
            IsBlocked = true,
            Priority = "Critical",
            CurrentStatus = "Blocked",
            NumberOfStatusChanges = 3
        }, 1);

        Assert.NotNull(result);
        Assert.True(result.RiskScore >= 0m);
    }
}
