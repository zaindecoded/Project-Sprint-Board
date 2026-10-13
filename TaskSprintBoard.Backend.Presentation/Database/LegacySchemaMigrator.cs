using System.Data;
using Microsoft.EntityFrameworkCore;
using TaskSprintBoard.Backend.Data.DbContext;

namespace TaskSprintBoard.Backend.Presentation.Database;

/// <summary>
/// Preserves the original integer-keyed database tables under the Legacy schema, then
/// copies their data into the current EF Core schema using GUID keys.
/// </summary>
public static class LegacySchemaMigrator
{
    private const string MigrationName = "IntegerKeySchemaToGuidSchemaV1";

    public static void ArchiveLegacyTables(TaskSprintBoardDbContext dbContext)
    {
        var connection = dbContext.Database.GetDbConnection();
        var closeWhenDone = connection.State != ConnectionState.Open;
        if (closeWhenDone)
            connection.Open();

        try
        {
            using (var probe = connection.CreateCommand())
            {
                probe.CommandText = """
                    SELECT CASE WHEN OBJECT_ID(N'dbo.Projects', N'U') IS NOT NULL
                                     AND COL_LENGTH(N'dbo.Projects', N'ProjectId') IS NOT NULL
                                     AND COL_LENGTH(N'dbo.Projects', N'Id') IS NULL
                                THEN 1 ELSE 0 END
                    """;
                if (Convert.ToInt32(probe.ExecuteScalar()) != 1)
                    return;
            }

            using var command = connection.CreateCommand();
            command.CommandText = """
                IF SCHEMA_ID(N'Legacy') IS NULL EXEC(N'CREATE SCHEMA Legacy');

                ALTER SCHEMA Legacy TRANSFER dbo.Users;
                ALTER SCHEMA Legacy TRANSFER dbo.Projects;
                ALTER SCHEMA Legacy TRANSFER dbo.Sprints;
                ALTER SCHEMA Legacy TRANSFER dbo.Tasks;
                ALTER SCHEMA Legacy TRANSFER dbo.Assignments;
                ALTER SCHEMA Legacy TRANSFER dbo.Comments;
                ALTER SCHEMA Legacy TRANSFER dbo.StatusHistory;
                ALTER SCHEMA Legacy TRANSFER dbo.TaskRiskEvaluations;

                IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NOT NULL
                    ALTER SCHEMA Legacy TRANSFER dbo.AuditLogs;
                IF OBJECT_ID(N'dbo.RiskTrainingExamples', N'U') IS NOT NULL
                    ALTER SCHEMA Legacy TRANSFER dbo.RiskTrainingExamples;
                """;
            command.ExecuteNonQuery();
        }
        finally
        {
            if (closeWhenDone)
                connection.Close();
        }
    }

    public static void ImportArchivedData(TaskSprintBoardDbContext dbContext)
    {
        var connection = dbContext.Database.GetDbConnection();
        var closeWhenDone = connection.State != ConnectionState.Open;
        if (closeWhenDone)
            connection.Open();

        try
        {
            using (var probe = connection.CreateCommand())
            {
                probe.CommandText = """
                    SELECT CASE WHEN SCHEMA_ID(N'Legacy') IS NOT NULL
                                     AND OBJECT_ID(N'Legacy.Users', N'U') IS NOT NULL
                                THEN 1 ELSE 0 END
                    """;
                if (Convert.ToInt32(probe.ExecuteScalar()) != 1)
                    return;
            }

            using var command = connection.CreateCommand();
            command.CommandText = """
                IF OBJECT_ID(N'Legacy.Users', N'U') IS NULL
                    RETURN;

                IF OBJECT_ID(N'Legacy.MigrationRuns', N'U') IS NULL
                BEGIN
                    CREATE TABLE Legacy.MigrationRuns
                    (
                        MigrationName nvarchar(100) NOT NULL PRIMARY KEY,
                        AppliedAt datetime2 NOT NULL
                    );
                END;

                IF EXISTS (SELECT 1 FROM Legacy.MigrationRuns WHERE MigrationName = @migrationName)
                    RETURN;

                SET XACT_ABORT ON;
                BEGIN TRY
                    BEGIN TRANSACTION;

                    IF OBJECT_ID(N'Legacy.IdMap', N'U') IS NULL
                    BEGIN
                        CREATE TABLE Legacy.IdMap
                        (
                            EntityType nvarchar(30) NOT NULL,
                            LegacyId bigint NOT NULL,
                            CurrentId uniqueidentifier NOT NULL,
                            CONSTRAINT PK_Legacy_IdMap PRIMARY KEY (EntityType, LegacyId),
                            CONSTRAINT UQ_Legacy_IdMap_CurrentId UNIQUE (CurrentId)
                        );
                    END;

                    INSERT INTO Legacy.IdMap (EntityType, LegacyId, CurrentId)
                    SELECT N'User', u.UserId, NEWID() FROM Legacy.Users u
                    WHERE NOT EXISTS (SELECT 1 FROM Legacy.IdMap m WHERE m.EntityType=N'User' AND m.LegacyId=u.UserId);
                    INSERT INTO Legacy.IdMap (EntityType, LegacyId, CurrentId)
                    SELECT N'Project', p.ProjectId, NEWID() FROM Legacy.Projects p
                    WHERE NOT EXISTS (SELECT 1 FROM Legacy.IdMap m WHERE m.EntityType=N'Project' AND m.LegacyId=p.ProjectId);
                    INSERT INTO Legacy.IdMap (EntityType, LegacyId, CurrentId)
                    SELECT N'Sprint', s.SprintId, NEWID() FROM Legacy.Sprints s
                    WHERE NOT EXISTS (SELECT 1 FROM Legacy.IdMap m WHERE m.EntityType=N'Sprint' AND m.LegacyId=s.SprintId);
                    INSERT INTO Legacy.IdMap (EntityType, LegacyId, CurrentId)
                    SELECT N'Task', t.TaskId, NEWID() FROM Legacy.Tasks t
                    WHERE NOT EXISTS (SELECT 1 FROM Legacy.IdMap m WHERE m.EntityType=N'Task' AND m.LegacyId=t.TaskId);
                    INSERT INTO Legacy.IdMap (EntityType, LegacyId, CurrentId)
                    SELECT N'Assignment', a.AssignmentId, NEWID() FROM Legacy.Assignments a
                    WHERE NOT EXISTS (SELECT 1 FROM Legacy.IdMap m WHERE m.EntityType=N'Assignment' AND m.LegacyId=a.AssignmentId);
                    INSERT INTO Legacy.IdMap (EntityType, LegacyId, CurrentId)
                    SELECT N'Comment', c.CommentId, NEWID() FROM Legacy.Comments c
                    WHERE NOT EXISTS (SELECT 1 FROM Legacy.IdMap m WHERE m.EntityType=N'Comment' AND m.LegacyId=c.CommentId);
                    INSERT INTO Legacy.IdMap (EntityType, LegacyId, CurrentId)
                    SELECT N'StatusHistory', h.StatusHistoryId, NEWID() FROM Legacy.StatusHistory h
                    WHERE NOT EXISTS (SELECT 1 FROM Legacy.IdMap m WHERE m.EntityType=N'StatusHistory' AND m.LegacyId=h.StatusHistoryId);
                    INSERT INTO Legacy.IdMap (EntityType, LegacyId, CurrentId)
                    SELECT N'TaskRiskEvaluation', e.EvaluationId, NEWID() FROM Legacy.TaskRiskEvaluations e
                    WHERE NOT EXISTS (SELECT 1 FROM Legacy.IdMap m WHERE m.EntityType=N'TaskRiskEvaluation' AND m.LegacyId=e.EvaluationId);

                    INSERT INTO dbo.Users
                        (Id, FirstName, LastName, Email, UserName, PasswordHash, Role, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsActive, IsDeleted)
                    SELECT um.CurrentId,
                        COALESCE(NULLIF(LEFT(LTRIM(RTRIM(u.DisplayName)), NULLIF(CHARINDEX(N' ', LTRIM(RTRIM(u.DisplayName)) + N' '), 0) - 1), N''), N'Legacy'),
                        COALESCE(NULLIF(LTRIM(RTRIM(SUBSTRING(LTRIM(RTRIM(u.DisplayName)), CHARINDEX(N' ', LTRIM(RTRIM(u.DisplayName)) + N' ') + 1, 150))), N''), N'User'),
                        u.Email, CONCAT(N'legacy-', u.UserId), u.PasswordHash, u.Role, u.CreatedAt,
                        COALESCE(creator.Email, N'legacy'), u.UpdatedAt, updater.Email, u.IsActive, u.IsDeleted
                    FROM Legacy.Users u
                    JOIN Legacy.IdMap um ON um.EntityType=N'User' AND um.LegacyId=u.UserId
                    LEFT JOIN Legacy.Users creator ON creator.UserId=u.CreatedBy
                    LEFT JOIN Legacy.Users updater ON updater.UserId=u.UpdatedBy;

                    INSERT INTO dbo.Projects
                        (Id, Name, Description, StartDate, EndDate, Status, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsActive, IsDeleted)
                    SELECT pm.CurrentId, p.Name, p.Description, CAST(p.StartDate AS datetime2), CAST(p.EndDate AS datetime2),
                        CASE WHEN p.IsActive=1 AND p.IsDeleted=0 THEN N'Active' ELSE N'Inactive' END,
                        p.CreatedAt, COALESCE(creator.Email,N'legacy'), p.UpdatedAt, updater.Email, p.IsActive, p.IsDeleted
                    FROM Legacy.Projects p
                    JOIN Legacy.IdMap pm ON pm.EntityType=N'Project' AND pm.LegacyId=p.ProjectId
                    LEFT JOIN Legacy.Users creator ON creator.UserId=p.CreatedBy
                    LEFT JOIN Legacy.Users updater ON updater.UserId=p.UpdatedBy;

                    INSERT INTO dbo.Sprints
                        (Id, ProjectId, Name, StartDate, EndDate, Goal, Status, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsActive, IsDeleted)
                    SELECT sm.CurrentId, pm.CurrentId, s.Name, CAST(s.StartDate AS datetime2), CAST(s.EndDate AS datetime2),
                        COALESCE(LEFT(s.Goal,500),N''),
                        CASE WHEN s.IsDeleted=1 OR s.IsActive=0 OR CAST(s.EndDate AS date)<CAST(GETUTCDATE() AS date) THEN N'Completed'
                             WHEN CAST(s.StartDate AS date)>CAST(GETUTCDATE() AS date) THEN N'Planned' ELSE N'Active' END,
                        s.CreatedAt, COALESCE(creator.Email,N'legacy'), s.UpdatedAt, updater.Email, s.IsActive, s.IsDeleted
                    FROM Legacy.Sprints s
                    JOIN Legacy.IdMap sm ON sm.EntityType=N'Sprint' AND sm.LegacyId=s.SprintId
                    JOIN Legacy.IdMap pm ON pm.EntityType=N'Project' AND pm.LegacyId=s.ProjectId
                    LEFT JOIN Legacy.Users creator ON creator.UserId=s.CreatedBy
                    LEFT JOIN Legacy.Users updater ON updater.UserId=s.UpdatedBy;

                    INSERT INTO dbo.Tasks
                        (Id, SprintId, Title, Description, Priority, Status, EstimateHours, ProgressPercentage, StartDate, DueDate, BlockerInformation,
                         CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsActive, IsDeleted)
                    SELECT tm.CurrentId, sm.CurrentId, t.Title, COALESCE(t.Description,N''), t.Priority,
                        CASE LTRIM(RTRIM(t.Status)) WHEN N'To Do' THEN N'Todo' WHEN N'Backlog' THEN N'Todo'
                             WHEN N'In Progress' THEN N'InProgress' ELSE t.Status END,
                        COALESCE(t.EstimateHours,0), CASE WHEN t.ProgressPercent>100 THEN 100 ELSE t.ProgressPercent END,
                        t.CreatedAt, CAST(t.DueDate AS datetime2),
                        CASE WHEN t.BlockerCount>0 THEN CONCAT(N'Legacy blocker count: ',t.BlockerCount) ELSE NULL END,
                        t.CreatedAt, COALESCE(creator.Email,N'legacy'), t.UpdatedAt, updater.Email, t.IsActive, t.IsDeleted
                    FROM Legacy.Tasks t
                    JOIN Legacy.IdMap tm ON tm.EntityType=N'Task' AND tm.LegacyId=t.TaskId
                    JOIN Legacy.IdMap sm ON sm.EntityType=N'Sprint' AND sm.LegacyId=t.SprintId
                    LEFT JOIN Legacy.Users creator ON creator.UserId=t.CreatedBy
                    LEFT JOIN Legacy.Users updater ON updater.UserId=t.UpdatedBy;

                    INSERT INTO dbo.Assignments
                        (Id, TaskId, UserId, AssignedAt, UnassignedAt, RoleInTask, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsActive, IsDeleted)
                    SELECT am.CurrentId, tm.CurrentId, um.CurrentId, a.CreatedAt, NULL, a.AssignmentRole,
                        a.CreatedAt, COALESCE(creator.Email,N'legacy'), a.UpdatedAt, updater.Email, a.IsActive, a.IsDeleted
                    FROM Legacy.Assignments a
                    JOIN Legacy.IdMap am ON am.EntityType=N'Assignment' AND am.LegacyId=a.AssignmentId
                    JOIN Legacy.IdMap tm ON tm.EntityType=N'Task' AND tm.LegacyId=a.TaskId
                    JOIN Legacy.IdMap um ON um.EntityType=N'User' AND um.LegacyId=a.UserId
                    LEFT JOIN Legacy.Users creator ON creator.UserId=a.CreatedBy
                    LEFT JOIN Legacy.Users updater ON updater.UserId=a.UpdatedBy;

                    INSERT INTO dbo.Comments
                        (Id, TaskId, UserId, Message, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsActive, IsDeleted)
                    SELECT cm.CurrentId, tm.CurrentId, um.CurrentId, c.Content, c.CreatedAt,
                        COALESCE(creator.Email,N'legacy'), c.UpdatedAt, updater.Email, c.IsActive, c.IsDeleted
                    FROM Legacy.Comments c
                    JOIN Legacy.IdMap cm ON cm.EntityType=N'Comment' AND cm.LegacyId=c.CommentId
                    JOIN Legacy.IdMap tm ON tm.EntityType=N'Task' AND tm.LegacyId=c.TaskId
                    JOIN Legacy.IdMap um ON um.EntityType=N'User' AND um.LegacyId=c.UserId
                    LEFT JOIN Legacy.Users creator ON creator.UserId=c.CreatedBy
                    LEFT JOIN Legacy.Users updater ON updater.UserId=c.UpdatedBy;

                    INSERT INTO dbo.StatusHistory
                        (Id, TaskId, OldStatus, NewStatus, ChangedBy, ChangedAt, Reason, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsActive, IsDeleted)
                    SELECT hm.CurrentId, tm.CurrentId,
                        CASE LTRIM(RTRIM(COALESCE(h.PreviousStatus,N'Unknown'))) WHEN N'To Do' THEN N'Todo' WHEN N'Backlog' THEN N'Todo'
                             WHEN N'In Progress' THEN N'InProgress' ELSE COALESCE(h.PreviousStatus,N'Unknown') END,
                        CASE LTRIM(RTRIM(h.NewStatus)) WHEN N'To Do' THEN N'Todo' WHEN N'Backlog' THEN N'Todo'
                             WHEN N'In Progress' THEN N'InProgress' ELSE h.NewStatus END,
                        COALESCE(changedBy.Email,N'legacy'), h.ChangedAt, NULL, h.CreatedAt,
                        COALESCE(creator.Email,N'legacy'), h.UpdatedAt, updater.Email, h.IsActive, h.IsDeleted
                    FROM Legacy.StatusHistory h
                    JOIN Legacy.IdMap hm ON hm.EntityType=N'StatusHistory' AND hm.LegacyId=h.StatusHistoryId
                    JOIN Legacy.IdMap tm ON tm.EntityType=N'Task' AND tm.LegacyId=h.TaskId
                    JOIN Legacy.Users changedBy ON changedBy.UserId=h.ChangedByUserId
                    LEFT JOIN Legacy.Users creator ON creator.UserId=h.CreatedBy
                    LEFT JOIN Legacy.Users updater ON updater.UserId=h.UpdatedBy;

                    INSERT INTO dbo.TaskRiskEvaluations
                        (Id, TaskId, EstimateHours, TaskAge, DaysRemaining, ProgressPercentage, IsBlocked, Priority, CurrentStatus,
                         NumberOfStatusChanges, RiskScore, RiskLevel, Confidence, RiskFactors, Explanation, EvaluatedAt, EvaluatedBy,
                         UserOverride, OverrideReason, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsActive, IsDeleted)
                    SELECT em.CurrentId, tm.CurrentId, COALESCE(e.EstimateHours,t.EstimateHours,0), e.AgeDays,
                        CASE WHEN t.DueDate IS NULL THEN 0 ELSE DATEDIFF(DAY,CAST(e.CreatedAt AS date),t.DueDate) END,
                        CASE WHEN e.ProgressPercent>100 THEN 100 ELSE e.ProgressPercent END,
                        CASE WHEN e.BlockerCount>0 OR t.Status=N'Blocked' THEN 1 ELSE 0 END,
                        t.Priority,
                        CASE LTRIM(RTRIM(t.Status)) WHEN N'To Do' THEN N'Todo' WHEN N'Backlog' THEN N'Todo'
                             WHEN N'In Progress' THEN N'InProgress' ELSE t.Status END,
                        (SELECT COUNT(*) FROM Legacy.StatusHistory h WHERE h.TaskId=e.TaskId), e.RiskScore, e.RiskLevel, e.Confidence,
                        LEFT(e.FactorsJson,2000), e.Explanation, e.CreatedAt, evaluator.Email,
                        e.IsConfirmed, e.OverrideNote, e.CreatedAt, COALESCE(creator.Email,N'legacy'), e.CreatedAt, creator.Email, 1, 0
                    FROM Legacy.TaskRiskEvaluations e
                    JOIN Legacy.IdMap em ON em.EntityType=N'TaskRiskEvaluation' AND em.LegacyId=e.EvaluationId
                    JOIN Legacy.IdMap tm ON tm.EntityType=N'Task' AND tm.LegacyId=e.TaskId
                    JOIN Legacy.Tasks t ON t.TaskId=e.TaskId
                    LEFT JOIN Legacy.Users evaluator ON evaluator.UserId=e.UserId
                    LEFT JOIN Legacy.Users creator ON creator.UserId=e.UserId;

                    INSERT INTO Legacy.MigrationRuns (MigrationName, AppliedAt) VALUES (@migrationName, SYSUTCDATETIME());
                    COMMIT TRANSACTION;
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;
                """;

            var migrationName = command.CreateParameter();
            migrationName.ParameterName = "@migrationName";
            migrationName.Value = MigrationName;
            command.Parameters.Add(migrationName);
            command.ExecuteNonQuery();
        }
        finally
        {
            if (closeWhenDone)
                connection.Close();
        }
    }
}
