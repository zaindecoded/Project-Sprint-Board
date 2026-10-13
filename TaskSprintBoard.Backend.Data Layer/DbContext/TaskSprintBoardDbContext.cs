using Microsoft.EntityFrameworkCore;
using TaskSprintBoard.Backend.Data.Entities;

namespace TaskSprintBoard.Backend.Data.DbContext;

public class TaskSprintBoardDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public TaskSprintBoardDbContext(DbContextOptions<TaskSprintBoardDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<StatusHistory> StatusHistory => Set<StatusHistory>();
    public DbSet<TaskRiskEvaluation> TaskRiskEvaluations => Set<TaskRiskEvaluation>();

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
            entity.Property(x => x.Role).IsRequired().HasMaxLength(50);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.UserName).IsUnique();
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
    }
}
