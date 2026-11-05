using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.Data.DbContext;

public class TaskSprintBoardDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public TaskSprintBoardDbContext(DbContextOptions<TaskSprintBoardDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RoleInTask> RoleInTasks => Set<RoleInTask>();
    public DbSet<Status> Statuses => Set<Status>();
    public DbSet<TaskPriorityLookup> TaskPriorities => Set<TaskPriorityLookup>();
    public DbSet<TaskStatusLookup> TaskStatuses => Set<TaskStatusLookup>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<StatusHistory> StatusHistory => Set<StatusHistory>();
    public DbSet<TaskRiskEvaluation> TaskRiskEvaluations => Set<TaskRiskEvaluation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AuditAction> AuditActions => Set<AuditAction>();
    public DbSet<AuditEntityType> AuditEntityTypes => Set<AuditEntityType>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AddAuditEntries();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AddAuditEntries();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void AddAuditEntries()
    {
        ChangeTracker.DetectChanges();
        var events = new List<AuditLog>();
        foreach (var entry in ChangeTracker.Entries().Where(x => x.Entity is not AuditLog && x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var name = entry.Metadata.ClrType.Name;
            if (name == nameof(StatusHistory)) continue;
            var values = new Dictionary<string, object?>();
            var changed = entry.State == EntityState.Added
                ? entry.Properties.Where(p => p.Metadata.PropertyInfo != null && p.Metadata.Name is not ("Id" or "CreatedAt" or "CreatedBy" or "UpdatedAt" or "UpdatedBy" or "IsActive" or "IsDeleted"))
                : entry.Properties.Where(p => p.IsModified && p.Metadata.PropertyInfo != null && p.Metadata.Name is not ("Id" or "CreatedAt" or "CreatedBy" or "UpdatedAt" or "UpdatedBy"));
            foreach (var property in changed)
            {
                if (IsSensitive(property.Metadata.Name)) continue;
                var current = entry.State == EntityState.Deleted ? property.OriginalValue : property.CurrentValue;
                if (entry.State == EntityState.Modified)
                    values[property.Metadata.Name] = new { old = property.OriginalValue, @new = current };
                else values[property.Metadata.Name] = current;
            }
            var actorProperty = entry.Metadata.FindProperty(entry.State == EntityState.Added ? "CreatedBy" : "UpdatedBy");
            var actor = actorProperty == null ? null : entry.Property(actorProperty.Name).CurrentValue;
            var action = entry.State switch
            {
                EntityState.Added => name == nameof(Assignment) ? "Assigned" : "Created",
                EntityState.Deleted => name == nameof(Assignment) ? "Unassigned" : "Deleted",
                _ when name == nameof(Assignment) && entry.Metadata.FindProperty("IsDeleted") is not null && entry.Property("IsDeleted").CurrentValue is true => "Unassigned",
                _ when name == nameof(Assignment) => "Assignment Updated",
                _ when entry.Metadata.FindProperty("IsDeleted") is not null && entry.Property("IsDeleted").CurrentValue is true => "Deleted",
                _ when name == nameof(TaskItem) && entry.Property("StatusId").IsModified => "Status Changed",
                _ => "Updated"
            };
            if (entry.State == EntityState.Modified && values.Count == 0 && action != "Unassigned") continue;
            events.Add(new AuditLog
            {
                OccurredAt = DateTime.UtcNow,
                ActorUserId = actor is int id && id > 0 ? id : null,
                ActionId = AuditLookupIds.Action(action),
                EntityTypeId = AuditLookupIds.Entity(name == nameof(TaskItem) ? "Task" : name),
                EntityId = entry.State == EntityState.Added || entry.Property("Id").IsTemporary ? null : Convert.ToInt32(entry.Property("Id").CurrentValue),
                Details = values.Count == 0 ? null : JsonSerializer.Serialize(values)
            });
        }
        if (events.Count > 0) AuditLogs.AddRange(events);
    }

    private static bool IsSensitive(string name) =>
        name.Contains("password", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("token", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("key", StringComparison.OrdinalIgnoreCase);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("Projects");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.HasOne(x => x.StatusDefinition).WithMany(x => x.Projects).HasForeignKey(x => x.StatusId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<Sprint>(entity =>
        {
            entity.ToTable("Sprints");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Goal).HasMaxLength(500);
            entity.HasOne(x => x.StatusDefinition).WithMany(x => x.Sprints).HasForeignKey(x => x.StatusId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.ProjectId, x.Name }).IsUnique();
            entity.HasOne(x => x.Project)
                .WithMany(x => x.Sprints)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Email).IsRequired().HasMaxLength(200);
            entity.Property(x => x.UserName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.RoleId).HasDefaultValue(3);
            entity.HasOne(x => x.RoleDefinition)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.UserName).IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasData(
                new Role { Id = 1, Name = "Admin" },
                new Role { Id = 2, Name = "Project Manager" },
                new Role { Id = 3, Name = "Project Member" });
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("Tasks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Ignore(x => x.Priority);
            entity.Ignore(x => x.Status);
            entity.HasOne(x => x.PriorityDefinition).WithMany(x => x.Tasks).HasForeignKey(x => x.PriorityId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.StatusDefinition).WithMany(x => x.Tasks).HasForeignKey(x => x.StatusId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.EstimateHours).HasPrecision(10, 2);
            entity.Property(x => x.BlockerInformation).HasMaxLength(1000);
            entity.HasIndex(x => x.SprintId);
            entity.HasOne(x => x.Sprint)
                .WithMany(x => x.Tasks)
                .HasForeignKey(x => x.SprintId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RoleInTask>(entity =>
        {
            entity.ToTable("RoleInTasks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasData(new RoleInTask { Id = 1, Name = "Reviewer" }, new RoleInTask { Id = 2, Name = "Observer" }, new RoleInTask { Id = 3, Name = "Assignee" });
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.ToTable("status");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasData(new Status { Id = 1, Name = "Planned" }, new Status { Id = 2, Name = "Active" }, new Status { Id = 3, Name = "OnHold" }, new Status { Id = 4, Name = "Completed" }, new Status { Id = 5, Name = "Cancelled" });
        });

        modelBuilder.Entity<TaskPriorityLookup>(entity =>
        {
            entity.ToTable("TaskPriorities");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(50);
            entity.HasData(new TaskPriorityLookup { Id = 1, Name = "Low" }, new TaskPriorityLookup { Id = 2, Name = "Medium" }, new TaskPriorityLookup { Id = 3, Name = "High" }, new TaskPriorityLookup { Id = 4, Name = "Critical" });
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<TaskStatusLookup>(entity =>
        {
            entity.ToTable("TaskStatuses");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(50);
            entity.HasData(new TaskStatusLookup { Id = 1, Name = "Todo" }, new TaskStatusLookup { Id = 2, Name = "InProgress" }, new TaskStatusLookup { Id = 3, Name = "Blocked" }, new TaskStatusLookup { Id = 4, Name = "InReview" }, new TaskStatusLookup { Id = 5, Name = "Done" }, new TaskStatusLookup { Id = 6, Name = "Cancelled" });
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.ToTable("Assignments");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TaskId, x.UserId }).IsUnique();
            entity.HasOne(x => x.RoleInTaskDefinition).WithMany(x => x.Assignments).HasForeignKey(x => x.RoleInTaskId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Task)
                .WithMany(x => x.Assignments)
                .HasForeignKey(x => x.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User)
                .WithMany(x => x.Assignments)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.ToTable("Comments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Message).IsRequired().HasMaxLength(2000);
            entity.HasOne(x => x.Task)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StatusHistory>(entity =>
        {
            entity.ToTable("StatusHistory");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ChangedBy).IsRequired().HasMaxLength(200);
            entity.HasOne(x => x.OldStatusDefinition).WithMany().HasForeignKey(x => x.OldStatusId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.NewStatusDefinition).WithMany().HasForeignKey(x => x.NewStatusId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Task)
                .WithMany(x => x.StatusHistory)
                .HasForeignKey(x => x.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskRiskEvaluation>(entity =>
        {
            entity.ToTable("TaskRiskEvaluations");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.PriorityDefinition).WithMany().HasForeignKey(x => x.PriorityId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CurrentStatusDefinition).WithMany().HasForeignKey(x => x.CurrentStatusId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.RiskLevel).HasMaxLength(50);
            entity.Property(x => x.EstimateHours).HasPrecision(10, 2);
            entity.Property(x => x.RiskScore).HasPrecision(5, 2);
            entity.Property(x => x.Confidence).HasPrecision(5, 2);
            entity.Property(x => x.RiskFactors).HasMaxLength(2000);
            entity.Property(x => x.Explanation).HasMaxLength(4000);
            entity.HasOne(x => x.Task)
                .WithMany(x => x.RiskEvaluations)
                .HasForeignKey(x => x.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ActorName).HasMaxLength(250);
            entity.Property(x => x.Details).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => x.OccurredAt);
            entity.HasIndex(x => new { x.ActionId, x.EntityTypeId, x.OccurredAt });
            entity.HasOne(x => x.ActionDefinition).WithMany(x => x.Logs).HasForeignKey(x => x.ActionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.EntityTypeDefinition).WithMany(x => x.Logs).HasForeignKey(x => x.EntityTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditAction>(entity =>
        {
            entity.ToTable("AuditActions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasData(
                new AuditAction { Id = 1, Name = "Created" },
                new AuditAction { Id = 2, Name = "Updated" },
                new AuditAction { Id = 3, Name = "Deleted" },
                new AuditAction { Id = 4, Name = "Status Changed" },
                new AuditAction { Id = 5, Name = "Assigned" },
                new AuditAction { Id = 6, Name = "Unassigned" },
                new AuditAction { Id = 7, Name = "Assignment Updated" },
                new AuditAction { Id = 8, Name = "Login" },
                new AuditAction { Id = 9, Name = "Other" });
        });

        modelBuilder.Entity<AuditEntityType>(entity =>
        {
            entity.ToTable("AuditEntityTypes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasData(
                new AuditEntityType { Id = 1, Name = "Task" },
                new AuditEntityType { Id = 2, Name = "Project" },
                new AuditEntityType { Id = 3, Name = "Sprint" },
                new AuditEntityType { Id = 4, Name = "User" },
                new AuditEntityType { Id = 5, Name = "Assignment" },
                new AuditEntityType { Id = 6, Name = "Comment" },
                new AuditEntityType { Id = 7, Name = "TaskRiskEvaluation" },
                new AuditEntityType { Id = 8, Name = "Role" },
                new AuditEntityType { Id = 9, Name = "Status" },
                new AuditEntityType { Id = 10, Name = "TaskPriorityLookup" },
                new AuditEntityType { Id = 11, Name = "TaskStatusLookup" },
                new AuditEntityType { Id = 12, Name = "RoleInTask" },
                new AuditEntityType { Id = 13, Name = "Other" },
                new AuditEntityType { Id = 14, Name = "Authentication" });
        });
    }
}
