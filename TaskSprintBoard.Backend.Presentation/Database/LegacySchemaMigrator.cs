using System.Data;
using Microsoft.EntityFrameworkCore;
using TaskSprintBoard.Backend.Data.DbContext;

namespace TaskSprintBoard.Backend.Presentation.Database;

/// <summary>
/// Preserves the original integer-keyed database tables under the Legacy schema, then
/// copies their data into the current EF Core schema while preserving integer keys.
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
                            CurrentId int NOT NULL,
                            CONSTRAINT PK_Legacy_IdMap PRIMARY KEY (EntityType, LegacyId)
                        );
                    END;

                    INSERT INTO Legacy.IdMap (EntityType, LegacyId, CurrentId)
                    SELECT N'User', u.UserId, CONVERT(int, u.UserId) FROM Legacy.Users u
                    WHERE NOT EXISTS (SELECT 1 FROM Legacy.IdMap m WHERE m.EntityType=N'User' AND m.LegacyId=u.UserId);
                    INSERT INTO Legacy.IdMap (EntityType, LegacyId, CurrentId)
                    SELECT N'Project', p.ProjectId, CONVERT(int, p.ProjectId) FROM Legacy.Projects p
                    WHERE NOT EXISTS (SELECT 1 FROM Legacy.IdMap m WHERE m.EntityType=N'Project' AND m.LegacyId=p.ProjectId);
                    INSERT INTO Legacy.IdMap (EntityType, LegacyId, CurrentId)
                    SELECT N'Sprint', s.SprintId, CONVERT(int, s.SprintId) FROM Legacy.Sprints s
                    WHERE NOT EXISTS (SELECT 1 FROM Legacy.IdMap m WHERE m.EntityType=N'Sprint' AND m.LegacyId=s.SprintId);
                    INSERT INTO Legacy.IdMap (EntityType, LegacyId, CurrentId)
                    SELECT N'Task', t.TaskId, CONVERT(int, t.TaskId) FROM Legacy.Tasks t
                    WHERE NOT EXISTS (SELECT 1 FROM Legacy.IdMap m WHERE m.EntityType=N'Task' AND m.LegacyId=t.TaskId);
                    INSERT INTO Legacy.IdMap (EntityType, LegacyId, CurrentId)
                    SELECT N'Assignment', a.AssignmentId, CONVERT(int, a.AssignmentId) FROM Legacy.Assignments a
                    WHERE NOT EXISTS (SELECT 1 FROM Legacy.IdMap m WHERE m.EntityType=N'Assignment' AND m.LegacyId=a.AssignmentId);
                    INSERT INTO Legacy.IdMap (EntityType, LegacyId, CurrentId)
                    SELECT N'Comment', c.CommentId, CONVERT(int, c.CommentId) FROM Legacy.Comments c
                    WHERE NOT EXISTS (SELECT 1 FROM Legacy.IdMap m WHERE m.EntityType=N'Comment' AND m.LegacyId=c.CommentId);
                    INSERT INTO Legacy.IdMap (EntityType, LegacyId, CurrentId)
                    SELECT N'StatusHistory', h.StatusHistoryId, CONVERT(int, h.StatusHistoryId) FROM Legacy.StatusHistory h
                    WHERE NOT EXISTS (SELECT 1 FROM Legacy.IdMap m WHERE m.EntityType=N'StatusHistory' AND m.LegacyId=h.StatusHistoryId);
                    INSERT INTO Legacy.IdMap (EntityType, LegacyId, CurrentId)
                    SELECT N'TaskRiskEvaluation', e.EvaluationId, CONVERT(int, e.EvaluationId) FROM Legacy.TaskRiskEvaluations e
                    WHERE NOT EXISTS (SELECT 1 FROM Legacy.IdMap m WHERE m.EntityType=N'TaskRiskEvaluation' AND m.LegacyId=e.EvaluationId);

                    SET IDENTITY_INSERT dbo.Users ON;
                    INSERT INTO dbo.Users
                        (Id, FirstName, LastName, Email, UserName, PasswordHash, RoleId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsActive, IsDeleted)
                    SELECT um.CurrentId,
                        COALESCE(NULLIF(LEFT(LTRIM(RTRIM(u.DisplayName)), NULLIF(CHARINDEX(N' ', LTRIM(RTRIM(u.DisplayName)) + N' '), 0) - 1), N''), N'Legacy'),
                        COALESCE(NULLIF(LTRIM(RTRIM(SUBSTRING(LTRIM(RTRIM(u.DisplayName)), CHARINDEX(N' ', LTRIM(RTRIM(u.DisplayName)) + N' ') + 1, 150))), N''), N'User'),
                        u.Email, CONCAT(N'legacy-', u.UserId), u.PasswordHash,
                        CASE WHEN u.Role IN (N'Admin', N'Administrator') THEN 1 WHEN u.Role IN (N'ProjectManager', N'Project Manager') THEN 2 ELSE 3 END, u.CreatedAt,
                        creatorMap.CurrentId, u.UpdatedAt, updaterMap.CurrentId, u.IsActive, u.IsDeleted
                    FROM Legacy.Users u
                    JOIN Legacy.IdMap um ON um.EntityType=N'User' AND um.LegacyId=u.UserId
                    LEFT JOIN Legacy.IdMap creatorMap ON creatorMap.EntityType=N'User' AND creatorMap.LegacyId=u.CreatedBy
                    LEFT JOIN Legacy.IdMap updaterMap ON updaterMap.EntityType=N'User' AND updaterMap.LegacyId=u.UpdatedBy;
                    SET IDENTITY_INSERT dbo.Users OFF;

                    INSERT INTO dbo.Projects
                        (Id, Name, Description, StartDate, EndDate, Status, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsActive, IsDeleted)
                    SELECT pm.CurrentId, p.Name, p.Description, CAST(p.StartDate AS datetime2), CAST(p.EndDate AS datetime2),
                        CASE WHEN p.IsActive=1 AND p.IsDeleted=0 THEN N'Active' ELSE N'Inactive' END,
                        p.CreatedAt, creatorMap.CurrentId, p.UpdatedAt, updaterMap.CurrentId, p.IsActive, p.IsDeleted
                    FROM Legacy.Projects p
                    JOIN Legacy.IdMap pm ON pm.EntityType=N'Project' AND pm.LegacyId=p.ProjectId
                    LEFT JOIN Legacy.IdMap creatorMap ON creatorMap.EntityType=N'User' AND creatorMap.LegacyId=p.CreatedBy
                    LEFT JOIN Legacy.IdMap updaterMap ON updaterMap.EntityType=N'User' AND updaterMap.LegacyId=p.UpdatedBy;

                    INSERT INTO dbo.Sprints
                        (Id, ProjectId, Name, StartDate, EndDate, Goal, Status, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsActive, IsDeleted)
                    SELECT sm.CurrentId, pm.CurrentId, s.Name, CAST(s.StartDate AS datetime2), CAST(s.EndDate AS datetime2),
                        COALESCE(LEFT(s.Goal,500),N''),
                        CASE WHEN s.IsDeleted=1 OR s.IsActive=0 OR CAST(s.EndDate AS date)<CAST(GETUTCDATE() AS date) THEN N'Completed'
                             WHEN CAST(s.StartDate AS date)>CAST(GETUTCDATE() AS date) THEN N'Planned' ELSE N'Active' END,
                        s.CreatedAt, creatorMap.CurrentId, s.UpdatedAt, updaterMap.CurrentId, s.IsActive, s.IsDeleted
                    FROM Legacy.Sprints s
                    JOIN Legacy.IdMap sm ON sm.EntityType=N'Sprint' AND sm.LegacyId=s.SprintId
                    JOIN Legacy.IdMap pm ON pm.EntityType=N'Project' AND pm.LegacyId=s.ProjectId
                    LEFT JOIN Legacy.IdMap creatorMap ON creatorMap.EntityType=N'User' AND creatorMap.LegacyId=s.CreatedBy
                    LEFT JOIN Legacy.IdMap updaterMap ON updaterMap.EntityType=N'User' AND updaterMap.LegacyId=s.UpdatedBy;

                    INSERT INTO dbo.Tasks
                        (Id, SprintId, Title, Description, Priority, Status, EstimateHours, ProgressPercentage, StartDate, DueDate, BlockerInformation,
                         CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsActive, IsDeleted)
                    SELECT tm.CurrentId, sm.CurrentId, t.Title, COALESCE(t.Description,N''), t.Priority,
                        CASE LTRIM(RTRIM(t.Status)) WHEN N'To Do' THEN N'Todo' WHEN N'Backlog' THEN N'Todo'
                             WHEN N'In Progress' THEN N'InProgress' ELSE t.Status END,
                        COALESCE(t.EstimateHours,0), CASE WHEN t.ProgressPercent>100 THEN 100 ELSE t.ProgressPercent END,
                        t.CreatedAt, CAST(t.DueDate AS datetime2),
                        CASE WHEN t.BlockerCount>0 THEN CONCAT(N'Legacy blocker count: ',t.BlockerCount) ELSE NULL END,
                        t.CreatedAt, creatorMap.CurrentId, t.UpdatedAt, updaterMap.CurrentId, t.IsActive, t.IsDeleted
                    FROM Legacy.Tasks t
                    JOIN Legacy.IdMap tm ON tm.EntityType=N'Task' AND tm.LegacyId=t.TaskId
                    JOIN Legacy.IdMap sm ON sm.EntityType=N'Sprint' AND sm.LegacyId=t.SprintId
                    LEFT JOIN Legacy.IdMap creatorMap ON creatorMap.EntityType=N'User' AND creatorMap.LegacyId=t.CreatedBy
                    LEFT JOIN Legacy.IdMap updaterMap ON updaterMap.EntityType=N'User' AND updaterMap.LegacyId=t.UpdatedBy;

                    INSERT INTO dbo.Assignments
                        (Id, TaskId, UserId, AssignedAt, UnassignedAt, RoleInTask, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsActive, IsDeleted)
                    SELECT am.CurrentId, tm.CurrentId, um.CurrentId, a.CreatedAt, NULL, a.AssignmentRole,
                        a.CreatedAt, creatorMap.CurrentId, a.UpdatedAt, updaterMap.CurrentId, a.IsActive, a.IsDeleted
                    FROM Legacy.Assignments a
                    JOIN Legacy.IdMap am ON am.EntityType=N'Assignment' AND am.LegacyId=a.AssignmentId
                    JOIN Legacy.IdMap tm ON tm.EntityType=N'Task' AND tm.LegacyId=a.TaskId
                    JOIN Legacy.IdMap um ON um.EntityType=N'User' AND um.LegacyId=a.UserId
                    LEFT JOIN Legacy.IdMap creatorMap ON creatorMap.EntityType=N'User' AND creatorMap.LegacyId=a.CreatedBy
                    LEFT JOIN Legacy.IdMap updaterMap ON updaterMap.EntityType=N'User' AND updaterMap.LegacyId=a.UpdatedBy;

                    INSERT INTO dbo.Comments
                        (Id, TaskId, UserId, Message, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsActive, IsDeleted)
                    SELECT cm.CurrentId, tm.CurrentId, um.CurrentId, c.Content, c.CreatedAt,
                        creatorMap.CurrentId, c.UpdatedAt, updaterMap.CurrentId, c.IsActive, c.IsDeleted
                    FROM Legacy.Comments c
                    JOIN Legacy.IdMap cm ON cm.EntityType=N'Comment' AND cm.LegacyId=c.CommentId
                    JOIN Legacy.IdMap tm ON tm.EntityType=N'Task' AND tm.LegacyId=c.TaskId
                    JOIN Legacy.IdMap um ON um.EntityType=N'User' AND um.LegacyId=c.UserId
                    LEFT JOIN Legacy.IdMap creatorMap ON creatorMap.EntityType=N'User' AND creatorMap.LegacyId=c.CreatedBy
                    LEFT JOIN Legacy.IdMap updaterMap ON updaterMap.EntityType=N'User' AND updaterMap.LegacyId=c.UpdatedBy;

                    INSERT INTO dbo.StatusHistory
                        (Id, TaskId, OldStatus, NewStatus, ChangedBy, ChangedAt, Reason, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsActive, IsDeleted)
                    SELECT hm.CurrentId, tm.CurrentId,
                        CASE LTRIM(RTRIM(COALESCE(h.PreviousStatus,N'Unknown'))) WHEN N'To Do' THEN N'Todo' WHEN N'Backlog' THEN N'Todo'
                             WHEN N'In Progress' THEN N'InProgress' ELSE COALESCE(h.PreviousStatus,N'Unknown') END,
                        CASE LTRIM(RTRIM(h.NewStatus)) WHEN N'To Do' THEN N'Todo' WHEN N'Backlog' THEN N'Todo'
                             WHEN N'In Progress' THEN N'InProgress' ELSE h.NewStatus END,
                        COALESCE(changedBy.Email,N'legacy'), h.ChangedAt, NULL, h.CreatedAt,
                        creatorMap.CurrentId, h.UpdatedAt, updaterMap.CurrentId, h.IsActive, h.IsDeleted
                    FROM Legacy.StatusHistory h
                    JOIN Legacy.IdMap hm ON hm.EntityType=N'StatusHistory' AND hm.LegacyId=h.StatusHistoryId
                    JOIN Legacy.IdMap tm ON tm.EntityType=N'Task' AND tm.LegacyId=h.TaskId
                    JOIN Legacy.Users changedBy ON changedBy.UserId=h.ChangedByUserId
                    LEFT JOIN Legacy.IdMap creatorMap ON creatorMap.EntityType=N'User' AND creatorMap.LegacyId=h.CreatedBy
                    LEFT JOIN Legacy.IdMap updaterMap ON updaterMap.EntityType=N'User' AND updaterMap.LegacyId=h.UpdatedBy;

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
                        e.IsConfirmed, e.OverrideNote, e.CreatedAt, creatorMap.CurrentId, e.CreatedAt, creatorMap.CurrentId, 1, 0
                    FROM Legacy.TaskRiskEvaluations e
                    JOIN Legacy.IdMap em ON em.EntityType=N'TaskRiskEvaluation' AND em.LegacyId=e.EvaluationId
                    JOIN Legacy.IdMap tm ON tm.EntityType=N'Task' AND tm.LegacyId=e.TaskId
                    JOIN Legacy.Tasks t ON t.TaskId=e.TaskId
                    LEFT JOIN Legacy.Users evaluator ON evaluator.UserId=e.UserId
                    LEFT JOIN Legacy.IdMap creatorMap ON creatorMap.EntityType=N'User' AND creatorMap.LegacyId=e.UserId;

                    INSERT INTO Legacy.MigrationRuns (MigrationName, AppliedAt) VALUES (@migrationName, SYSUTCDATETIME());
                    DECLARE @nextId int, @sequenceSql nvarchar(100);
                    SELECT @nextId = ISNULL(MAX(Id), 0) + 1 FROM dbo.Projects; SET @sequenceSql = N'ALTER SEQUENCE dbo.ProjectIds RESTART WITH ' + CONVERT(nvarchar(20), @nextId); EXEC sys.sp_executesql @sequenceSql;
                    SELECT @nextId = ISNULL(MAX(Id), 0) + 1 FROM dbo.Sprints; SET @sequenceSql = N'ALTER SEQUENCE dbo.SprintIds RESTART WITH ' + CONVERT(nvarchar(20), @nextId); EXEC sys.sp_executesql @sequenceSql;
                    SELECT @nextId = ISNULL(MAX(Id), 0) + 1 FROM dbo.Tasks; SET @sequenceSql = N'ALTER SEQUENCE dbo.TaskIds RESTART WITH ' + CONVERT(nvarchar(20), @nextId); EXEC sys.sp_executesql @sequenceSql;
                    SELECT @nextId = ISNULL(MAX(Id), 0) + 1 FROM dbo.Assignments; SET @sequenceSql = N'ALTER SEQUENCE dbo.AssignmentIds RESTART WITH ' + CONVERT(nvarchar(20), @nextId); EXEC sys.sp_executesql @sequenceSql;
                    SELECT @nextId = ISNULL(MAX(Id), 0) + 1 FROM dbo.Comments; SET @sequenceSql = N'ALTER SEQUENCE dbo.CommentIds RESTART WITH ' + CONVERT(nvarchar(20), @nextId); EXEC sys.sp_executesql @sequenceSql;
                    SELECT @nextId = ISNULL(MAX(Id), 0) + 1 FROM dbo.StatusHistory; SET @sequenceSql = N'ALTER SEQUENCE dbo.StatusHistoryIds RESTART WITH ' + CONVERT(nvarchar(20), @nextId); EXEC sys.sp_executesql @sequenceSql;
                    SELECT @nextId = ISNULL(MAX(Id), 0) + 1 FROM dbo.TaskRiskEvaluations; SET @sequenceSql = N'ALTER SEQUENCE dbo.TaskRiskEvaluationIds RESTART WITH ' + CONVERT(nvarchar(20), @nextId); EXEC sys.sp_executesql @sequenceSql;
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
