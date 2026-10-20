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
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<StatusHistory> StatusHistory => Set<StatusHistory>();
    public DbSet<TaskRiskEvaluation> TaskRiskEvaluations => Set<TaskRiskEvaluation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

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
                _ when name == nameof(TaskItem) && entry.Property("Status").IsModified => "Status Changed",
                _ => "Updated"
            };
            if (entry.State == EntityState.Modified && values.Count == 0 && action != "Unassigned") continue;
            events.Add(new AuditLog
            {
                OccurredAt = DateTime.UtcNow,
                ActorUserId = actor is int id && id > 0 ? id : null,
                Action = action,
                EntityType = name == nameof(TaskItem) ? "Task" : name,
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
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<Sprint>(entity =>
        {
            entity.ToTable("Sprints");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Goal).HasMaxLength(500);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(50);
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
            entity.Property(x => x.Priority).IsRequired().HasMaxLength(50);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(50);
            entity.Property(x => x.EstimateHours).HasPrecision(10, 2);
            entity.Property(x => x.BlockerInformation).HasMaxLength(1000);
            entity.HasIndex(x => x.SprintId);
            entity.HasOne(x => x.Sprint)
                .WithMany(x => x.Tasks)
                .HasForeignKey(x => x.SprintId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.ToTable("Assignments");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TaskId, x.UserId }).IsUnique();
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
            entity.Property(x => x.OldStatus).IsRequired().HasMaxLength(50);
            entity.Property(x => x.NewStatus).IsRequired().HasMaxLength(50);
            entity.Property(x => x.ChangedBy).IsRequired().HasMaxLength(200);
            entity.HasOne(x => x.Task)
                .WithMany(x => x.StatusHistory)
                .HasForeignKey(x => x.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskRiskEvaluation>(entity =>
        {
            entity.ToTable("TaskRiskEvaluations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Priority).HasMaxLength(50);
            entity.Property(x => x.CurrentStatus).HasMaxLength(50);
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
            entity.Property(x => x.Action).IsRequired().HasMaxLength(100);
            entity.Property(x => x.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(x => x.ActorName).HasMaxLength(250);
            entity.Property(x => x.Details).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => x.OccurredAt);
            entity.HasIndex(x => new { x.Action, x.EntityType, x.OccurredAt });
        });
    }
}
