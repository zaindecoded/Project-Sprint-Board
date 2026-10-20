using TaskSprintBoard.Backend.Data.DbContext;
using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.Data.Seed;

public static class DatabaseSeed
{
    public static void Seed(TaskSprintBoardDbContext context)
    {
        if (context.Database.CanConnect())
        {
            if (!context.Users.Any())
            {
                var users = new[]
                {
                    new User { FirstName = "Alice", LastName = "Adams", Email = "alice@company.com", UserName = "alice", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"), Role = "Admin", RoleId = 1, CreatedBy = null },
                    new User { FirstName = "Brian", LastName = "Baker", Email = "brian@company.com", UserName = "brian", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager123!"), Role = "ProjectManager", RoleId = 2, CreatedBy = null },
                    new User { FirstName = "Cara", LastName = "Clark", Email = "cara@company.com", UserName = "cara", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Member123!"), Role = "TeamMember", RoleId = 3, CreatedBy = null },
                    new User { FirstName = "David", LastName = "Dunne", Email = "david@company.com", UserName = "david", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Member123!"), Role = "TeamMember", RoleId = 3, CreatedBy = null },
                    new User { FirstName = "Ella", LastName = "Evans", Email = "ella@company.com", UserName = "ella", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager123!"), Role = "ProjectManager", RoleId = 2, CreatedBy = null }
                };

                context.Users.AddRange(users);
                context.SaveChanges();
            }

            // Provide the local Development login when it does not conflict with an
            // existing account. Preserve credentials and roles for migrated users.
            var developmentAdmin = context.Users.SingleOrDefault(user => user.Email == "alice@company.com");
            if (developmentAdmin == null)
            {
                developmentAdmin = new User
                {
                    FirstName = "Alice",
                    LastName = "Adams",
                    Email = "alice@company.com",
                    UserName = "alice",
                    Role = "Admin",
                    RoleId = 1,
                    CreatedBy = null
                };
                context.Users.Add(developmentAdmin);
                developmentAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
                context.SaveChanges();
            }

            if (!context.Projects.Any())
            {
                var project = new Project
                {
                    Name = "Task Sprint Board",
                    Description = "Core planning project for sprint execution and task tracking.",
                    Status = "Active",
                    CreatedBy = null
                };

                var sprint = new Sprint
                {
                    Name = "Sprint 1",
                    Goal = "Deliver core task management features.",
                    StartDate = DateTime.UtcNow.AddDays(-12),
                    EndDate = DateTime.UtcNow.AddDays(18),
                    Status = "Active",
                    CreatedBy = null,
                    Project = project
                };

                project.Sprints.Add(sprint);

                var task1 = new TaskItem
                {
                    Title = "Set up project backend",
                    Description = "Create the .NET backend and database structure.",
                    Priority = "High",
                    Status = "InProgress",
                    EstimateHours = 24,
                    ProgressPercentage = 65,
                    StartDate = DateTime.UtcNow.AddDays(-8),
                    DueDate = DateTime.UtcNow.AddDays(5),
                    BlockerInformation = "Waiting on review feedback.",
                    CreatedBy = null,
                    Sprint = sprint
                };

                var task2 = new TaskItem
                {
                    Title = "Build task workflow",
                    Description = "Implement task status transitions and history logging.",
                    Priority = "Critical",
                    Status = "Todo",
                    EstimateHours = 18,
                    ProgressPercentage = 15,
                    StartDate = DateTime.UtcNow.AddDays(-3),
                    DueDate = DateTime.UtcNow.AddDays(7),
                    CreatedBy = null,
                    Sprint = sprint
                };

                sprint.Tasks.Add(task1);
                sprint.Tasks.Add(task2);

                var users = context.Users.ToList();
                if (users.Count >= 3)
                {
                    task1.Assignments.Add(new Assignment { User = users[0], AssignedAt = DateTime.UtcNow.AddDays(-5), RoleInTask = "Owner", CreatedBy = null });
                    task1.Assignments.Add(new Assignment { User = users[2], AssignedAt = DateTime.UtcNow.AddDays(-2), RoleInTask = "Contributor", CreatedBy = null });
                }

                if (users.Count >= 2)
                {
                    task2.Assignments.Add(new Assignment { User = users[1], AssignedAt = DateTime.UtcNow.AddDays(-1), RoleInTask = "Owner", CreatedBy = null });
                }

                task1.Comments.Add(new Comment { User = users[0], Message = "Initial backend setup is underway.", CreatedBy = null });
                task1.StatusHistory.Add(new StatusHistory { OldStatus = "Todo", NewStatus = "InProgress", ChangedBy = "alice", ChangedAt = DateTime.UtcNow.AddDays(-2), Reason = "Work started.", CreatedBy = null });
                task1.RiskEvaluations.Add(new TaskRiskEvaluation
                {
                    EstimateHours = 24,
                    TaskAge = 8,
                    DaysRemaining = 5,
                    ProgressPercentage = 65,
                    IsBlocked = true,
                    Priority = "High",
                    CurrentStatus = "InProgress",
                    NumberOfStatusChanges = 2,
                    RiskScore = 72,
                    RiskLevel = "High",
                    Confidence = 78,
                    RiskFactors = "Task is blocked; Low progress; Due date approaching",
                    Explanation = "This task is behind schedule and blocked by review feedback.",
                    EvaluatedAt = DateTime.UtcNow,
                    EvaluatedBy = "alice",
                    CreatedBy = null
                });

                context.Projects.Add(project);
            }

            context.SaveChanges();
        }
    }
}
