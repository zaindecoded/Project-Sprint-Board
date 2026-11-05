using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskSprintBoard.Backend.Data.Migrations;

public partial class ConvertDomainIdsToInt : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE #ProjectMap (OldId uniqueidentifier NOT NULL PRIMARY KEY, NewId int NOT NULL);
            CREATE TABLE #SprintMap (OldId uniqueidentifier NOT NULL PRIMARY KEY, NewId int NOT NULL);
            CREATE TABLE #TaskMap (OldId uniqueidentifier NOT NULL PRIMARY KEY, NewId int NOT NULL);
            CREATE TABLE #AssignmentMap (OldId uniqueidentifier NOT NULL PRIMARY KEY, NewId int NOT NULL);
            CREATE TABLE #CommentMap (OldId uniqueidentifier NOT NULL PRIMARY KEY, NewId int NOT NULL);
            CREATE TABLE #HistoryMap (OldId uniqueidentifier NOT NULL PRIMARY KEY, NewId int NOT NULL);
            CREATE TABLE #RiskMap (OldId uniqueidentifier NOT NULL PRIMARY KEY, NewId int NOT NULL);

            INSERT #ProjectMap SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) FROM dbo.Projects;
            INSERT #SprintMap SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) FROM dbo.Sprints;
            INSERT #TaskMap SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) FROM dbo.Tasks;
            INSERT #AssignmentMap SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) FROM dbo.Assignments;
            INSERT #CommentMap SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) FROM dbo.Comments;
            INSERT #HistoryMap SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) FROM dbo.StatusHistory;
            INSERT #RiskMap SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) FROM dbo.TaskRiskEvaluations;

            DECLARE @sql nvarchar(max) = N'';
            SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(name) + N';'
            FROM sys.foreign_keys
            WHERE parent_object_id IN (OBJECT_ID(N'dbo.Projects'), OBJECT_ID(N'dbo.Sprints'), OBJECT_ID(N'dbo.Tasks'), OBJECT_ID(N'dbo.Assignments'), OBJECT_ID(N'dbo.Comments'), OBJECT_ID(N'dbo.StatusHistory'), OBJECT_ID(N'dbo.TaskRiskEvaluations'))
               OR referenced_object_id IN (OBJECT_ID(N'dbo.Projects'), OBJECT_ID(N'dbo.Sprints'), OBJECT_ID(N'dbo.Tasks'), OBJECT_ID(N'dbo.Assignments'), OBJECT_ID(N'dbo.Comments'), OBJECT_ID(N'dbo.StatusHistory'), OBJECT_ID(N'dbo.TaskRiskEvaluations'));
            IF LEN(@sql) > 0 EXEC sys.sp_executesql @sql;

            SET @sql = N'';
            SELECT @sql += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(name) + N';'
            FROM sys.key_constraints
            WHERE parent_object_id IN (OBJECT_ID(N'dbo.Projects'), OBJECT_ID(N'dbo.Sprints'), OBJECT_ID(N'dbo.Tasks'), OBJECT_ID(N'dbo.Assignments'), OBJECT_ID(N'dbo.Comments'), OBJECT_ID(N'dbo.StatusHistory'), OBJECT_ID(N'dbo.TaskRiskEvaluations'));
            IF LEN(@sql) > 0 EXEC sys.sp_executesql @sql;

            SET @sql = N'';
            SELECT @sql += N'DROP INDEX ' + QUOTENAME(i.name) + N' ON ' + QUOTENAME(OBJECT_SCHEMA_NAME(i.object_id)) + N'.' + QUOTENAME(OBJECT_NAME(i.object_id)) + N';'
            FROM sys.indexes i
            WHERE i.object_id IN (OBJECT_ID(N'dbo.Projects'), OBJECT_ID(N'dbo.Sprints'), OBJECT_ID(N'dbo.Tasks'), OBJECT_ID(N'dbo.Assignments'), OBJECT_ID(N'dbo.Comments'), OBJECT_ID(N'dbo.StatusHistory'), OBJECT_ID(N'dbo.TaskRiskEvaluations'))
              AND i.index_id > 0 AND i.is_primary_key = 0 AND i.is_unique_constraint = 0;
            IF LEN(@sql) > 0 EXEC sys.sp_executesql @sql;
            """);

        migrationBuilder.Sql("""
            ALTER TABLE dbo.Projects ADD IdInt int NULL;
            ALTER TABLE dbo.Sprints ADD IdInt int NULL, ProjectIdInt int NULL;
            ALTER TABLE dbo.Tasks ADD IdInt int NULL, SprintIdInt int NULL;
            ALTER TABLE dbo.Assignments ADD IdInt int NULL, TaskIdInt int NULL;
            ALTER TABLE dbo.Comments ADD IdInt int NULL, TaskIdInt int NULL;
            ALTER TABLE dbo.StatusHistory ADD IdInt int NULL, TaskIdInt int NULL;
            ALTER TABLE dbo.TaskRiskEvaluations ADD IdInt int NULL, TaskIdInt int NULL;
            """);

        migrationBuilder.Sql("""
            UPDATE p SET IdInt = m.NewId FROM dbo.Projects p JOIN #ProjectMap m ON m.OldId = p.Id;
            UPDATE s SET IdInt = sm.NewId, ProjectIdInt = pm.NewId FROM dbo.Sprints s JOIN #SprintMap sm ON sm.OldId = s.Id JOIN #ProjectMap pm ON pm.OldId = s.ProjectId;
            UPDATE t SET IdInt = tm.NewId, SprintIdInt = sm.NewId FROM dbo.Tasks t JOIN #TaskMap tm ON tm.OldId = t.Id JOIN #SprintMap sm ON sm.OldId = t.SprintId;
            UPDATE a SET IdInt = am.NewId, TaskIdInt = tm.NewId FROM dbo.Assignments a JOIN #AssignmentMap am ON am.OldId = a.Id JOIN #TaskMap tm ON tm.OldId = a.TaskId;
            UPDATE c SET IdInt = cm.NewId, TaskIdInt = tm.NewId FROM dbo.Comments c JOIN #CommentMap cm ON cm.OldId = c.Id JOIN #TaskMap tm ON tm.OldId = c.TaskId;
            UPDATE h SET IdInt = hm.NewId, TaskIdInt = tm.NewId FROM dbo.StatusHistory h JOIN #HistoryMap hm ON hm.OldId = h.Id JOIN #TaskMap tm ON tm.OldId = h.TaskId;
            UPDATE e SET IdInt = em.NewId, TaskIdInt = tm.NewId FROM dbo.TaskRiskEvaluations e JOIN #RiskMap em ON em.OldId = e.Id JOIN #TaskMap tm ON tm.OldId = e.TaskId;

            IF EXISTS (SELECT 1 FROM dbo.Projects WHERE IdInt IS NULL)
               OR EXISTS (SELECT 1 FROM dbo.Sprints WHERE IdInt IS NULL OR ProjectIdInt IS NULL)
               OR EXISTS (SELECT 1 FROM dbo.Tasks WHERE IdInt IS NULL OR SprintIdInt IS NULL)
               OR EXISTS (SELECT 1 FROM dbo.Assignments WHERE IdInt IS NULL OR TaskIdInt IS NULL)
               OR EXISTS (SELECT 1 FROM dbo.Comments WHERE IdInt IS NULL OR TaskIdInt IS NULL)
               OR EXISTS (SELECT 1 FROM dbo.StatusHistory WHERE IdInt IS NULL OR TaskIdInt IS NULL)
               OR EXISTS (SELECT 1 FROM dbo.TaskRiskEvaluations WHERE IdInt IS NULL OR TaskIdInt IS NULL)
                THROW 51011, 'Could not map all domain IDs and relationships to integers.', 1;
            """);

        migrationBuilder.Sql("""
            ALTER TABLE dbo.Projects DROP COLUMN Id;
            ALTER TABLE dbo.Sprints DROP COLUMN Id, ProjectId;
            ALTER TABLE dbo.Tasks DROP COLUMN Id, SprintId;
            ALTER TABLE dbo.Assignments DROP COLUMN Id, TaskId;
            ALTER TABLE dbo.Comments DROP COLUMN Id, TaskId;
            ALTER TABLE dbo.StatusHistory DROP COLUMN Id, TaskId;
            ALTER TABLE dbo.TaskRiskEvaluations DROP COLUMN Id, TaskId;
            """);

        migrationBuilder.Sql("""
            EXEC sys.sp_rename N'dbo.Projects.IdInt', N'Id', N'COLUMN';
            EXEC sys.sp_rename N'dbo.Sprints.IdInt', N'Id', N'COLUMN';
            EXEC sys.sp_rename N'dbo.Sprints.ProjectIdInt', N'ProjectId', N'COLUMN';
            EXEC sys.sp_rename N'dbo.Tasks.IdInt', N'Id', N'COLUMN';
            EXEC sys.sp_rename N'dbo.Tasks.SprintIdInt', N'SprintId', N'COLUMN';
            EXEC sys.sp_rename N'dbo.Assignments.IdInt', N'Id', N'COLUMN';
            EXEC sys.sp_rename N'dbo.Assignments.TaskIdInt', N'TaskId', N'COLUMN';
            EXEC sys.sp_rename N'dbo.Comments.IdInt', N'Id', N'COLUMN';
            EXEC sys.sp_rename N'dbo.Comments.TaskIdInt', N'TaskId', N'COLUMN';
            EXEC sys.sp_rename N'dbo.StatusHistory.IdInt', N'Id', N'COLUMN';
            EXEC sys.sp_rename N'dbo.StatusHistory.TaskIdInt', N'TaskId', N'COLUMN';
            EXEC sys.sp_rename N'dbo.TaskRiskEvaluations.IdInt', N'Id', N'COLUMN';
            EXEC sys.sp_rename N'dbo.TaskRiskEvaluations.TaskIdInt', N'TaskId', N'COLUMN';
            """);

        migrationBuilder.Sql("""
            ALTER TABLE dbo.Projects ALTER COLUMN Id int NOT NULL;
            ALTER TABLE dbo.Sprints ALTER COLUMN Id int NOT NULL;
            ALTER TABLE dbo.Sprints ALTER COLUMN ProjectId int NOT NULL;
            ALTER TABLE dbo.Tasks ALTER COLUMN Id int NOT NULL;
            ALTER TABLE dbo.Tasks ALTER COLUMN SprintId int NOT NULL;
            ALTER TABLE dbo.Assignments ALTER COLUMN Id int NOT NULL;
            ALTER TABLE dbo.Assignments ALTER COLUMN TaskId int NOT NULL;
            ALTER TABLE dbo.Comments ALTER COLUMN Id int NOT NULL;
            ALTER TABLE dbo.Comments ALTER COLUMN TaskId int NOT NULL;
            ALTER TABLE dbo.StatusHistory ALTER COLUMN Id int NOT NULL;
            ALTER TABLE dbo.StatusHistory ALTER COLUMN TaskId int NOT NULL;
            ALTER TABLE dbo.TaskRiskEvaluations ALTER COLUMN Id int NOT NULL;
            ALTER TABLE dbo.TaskRiskEvaluations ALTER COLUMN TaskId int NOT NULL;
            """);

        migrationBuilder.Sql("""
            ALTER TABLE dbo.Projects ADD CONSTRAINT PK_Projects PRIMARY KEY (Id);
            ALTER TABLE dbo.Sprints ADD CONSTRAINT PK_Sprints PRIMARY KEY (Id);
            ALTER TABLE dbo.Tasks ADD CONSTRAINT PK_Tasks PRIMARY KEY (Id);
            ALTER TABLE dbo.Assignments ADD CONSTRAINT PK_Assignments PRIMARY KEY (Id);
            ALTER TABLE dbo.Comments ADD CONSTRAINT PK_Comments PRIMARY KEY (Id);
            ALTER TABLE dbo.StatusHistory ADD CONSTRAINT PK_StatusHistory PRIMARY KEY (Id);
            ALTER TABLE dbo.TaskRiskEvaluations ADD CONSTRAINT PK_TaskRiskEvaluations PRIMARY KEY (Id);
            """);

        migrationBuilder.Sql("""
            CREATE SEQUENCE dbo.ProjectIds AS int START WITH 1 INCREMENT BY 1;
            CREATE SEQUENCE dbo.SprintIds AS int START WITH 1 INCREMENT BY 1;
            CREATE SEQUENCE dbo.TaskIds AS int START WITH 1 INCREMENT BY 1;
            CREATE SEQUENCE dbo.AssignmentIds AS int START WITH 1 INCREMENT BY 1;
            CREATE SEQUENCE dbo.CommentIds AS int START WITH 1 INCREMENT BY 1;
            CREATE SEQUENCE dbo.StatusHistoryIds AS int START WITH 1 INCREMENT BY 1;
            CREATE SEQUENCE dbo.TaskRiskEvaluationIds AS int START WITH 1 INCREMENT BY 1;

            DECLARE @next int, @sql nvarchar(100);
            SELECT @next = ISNULL(MAX(Id), 0) + 1 FROM dbo.Projects; SET @sql = N'ALTER SEQUENCE dbo.ProjectIds RESTART WITH ' + CONVERT(nvarchar(20), @next); EXEC sys.sp_executesql @sql;
            SELECT @next = ISNULL(MAX(Id), 0) + 1 FROM dbo.Sprints; SET @sql = N'ALTER SEQUENCE dbo.SprintIds RESTART WITH ' + CONVERT(nvarchar(20), @next); EXEC sys.sp_executesql @sql;
            SELECT @next = ISNULL(MAX(Id), 0) + 1 FROM dbo.Tasks; SET @sql = N'ALTER SEQUENCE dbo.TaskIds RESTART WITH ' + CONVERT(nvarchar(20), @next); EXEC sys.sp_executesql @sql;
            SELECT @next = ISNULL(MAX(Id), 0) + 1 FROM dbo.Assignments; SET @sql = N'ALTER SEQUENCE dbo.AssignmentIds RESTART WITH ' + CONVERT(nvarchar(20), @next); EXEC sys.sp_executesql @sql;
            SELECT @next = ISNULL(MAX(Id), 0) + 1 FROM dbo.Comments; SET @sql = N'ALTER SEQUENCE dbo.CommentIds RESTART WITH ' + CONVERT(nvarchar(20), @next); EXEC sys.sp_executesql @sql;
            SELECT @next = ISNULL(MAX(Id), 0) + 1 FROM dbo.StatusHistory; SET @sql = N'ALTER SEQUENCE dbo.StatusHistoryIds RESTART WITH ' + CONVERT(nvarchar(20), @next); EXEC sys.sp_executesql @sql;
            SELECT @next = ISNULL(MAX(Id), 0) + 1 FROM dbo.TaskRiskEvaluations; SET @sql = N'ALTER SEQUENCE dbo.TaskRiskEvaluationIds RESTART WITH ' + CONVERT(nvarchar(20), @next); EXEC sys.sp_executesql @sql;

            ALTER TABLE dbo.Projects ADD CONSTRAINT DF_Projects_Id DEFAULT (NEXT VALUE FOR dbo.ProjectIds) FOR Id;
            ALTER TABLE dbo.Sprints ADD CONSTRAINT DF_Sprints_Id DEFAULT (NEXT VALUE FOR dbo.SprintIds) FOR Id;
            ALTER TABLE dbo.Tasks ADD CONSTRAINT DF_Tasks_Id DEFAULT (NEXT VALUE FOR dbo.TaskIds) FOR Id;
            ALTER TABLE dbo.Assignments ADD CONSTRAINT DF_Assignments_Id DEFAULT (NEXT VALUE FOR dbo.AssignmentIds) FOR Id;
            ALTER TABLE dbo.Comments ADD CONSTRAINT DF_Comments_Id DEFAULT (NEXT VALUE FOR dbo.CommentIds) FOR Id;
            ALTER TABLE dbo.StatusHistory ADD CONSTRAINT DF_StatusHistory_Id DEFAULT (NEXT VALUE FOR dbo.StatusHistoryIds) FOR Id;
            ALTER TABLE dbo.TaskRiskEvaluations ADD CONSTRAINT DF_TaskRiskEvaluations_Id DEFAULT (NEXT VALUE FOR dbo.TaskRiskEvaluationIds) FOR Id;

            CREATE UNIQUE INDEX IX_Projects_Name ON dbo.Projects(Name);
            CREATE INDEX IX_Projects_CreatedAt ON dbo.Projects(CreatedAt);
            CREATE UNIQUE INDEX IX_Sprints_ProjectId_Name ON dbo.Sprints(ProjectId, Name);
            CREATE INDEX IX_Tasks_SprintId ON dbo.Tasks(SprintId);
            CREATE INDEX IX_Assignments_UserId ON dbo.Assignments(UserId);
            CREATE UNIQUE INDEX IX_Assignments_TaskId_UserId ON dbo.Assignments(TaskId, UserId);
            CREATE INDEX IX_Comments_UserId ON dbo.Comments(UserId);
            CREATE INDEX IX_StatusHistory_TaskId ON dbo.StatusHistory(TaskId);
            CREATE INDEX IX_TaskRiskEvaluations_TaskId ON dbo.TaskRiskEvaluations(TaskId);

            ALTER TABLE dbo.Sprints ADD CONSTRAINT FK_Sprints_Projects_ProjectId FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(Id) ON DELETE CASCADE;
            ALTER TABLE dbo.Tasks ADD CONSTRAINT FK_Tasks_Sprints_SprintId FOREIGN KEY (SprintId) REFERENCES dbo.Sprints(Id) ON DELETE CASCADE;
            ALTER TABLE dbo.Assignments ADD CONSTRAINT FK_Assignments_Tasks_TaskId FOREIGN KEY (TaskId) REFERENCES dbo.Tasks(Id) ON DELETE CASCADE;
            ALTER TABLE dbo.Assignments ADD CONSTRAINT FK_Assignments_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE;
            ALTER TABLE dbo.Comments ADD CONSTRAINT FK_Comments_Tasks_TaskId FOREIGN KEY (TaskId) REFERENCES dbo.Tasks(Id) ON DELETE CASCADE;
            ALTER TABLE dbo.Comments ADD CONSTRAINT FK_Comments_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION;
            ALTER TABLE dbo.StatusHistory ADD CONSTRAINT FK_StatusHistory_Tasks_TaskId FOREIGN KEY (TaskId) REFERENCES dbo.Tasks(Id) ON DELETE CASCADE;
            ALTER TABLE dbo.TaskRiskEvaluations ADD CONSTRAINT FK_TaskRiskEvaluations_Tasks_TaskId FOREIGN KEY (TaskId) REFERENCES dbo.Tasks(Id) ON DELETE CASCADE;

            DROP TABLE #ProjectMap, #SprintMap, #TaskMap, #AssignmentMap, #CommentMap, #HistoryMap, #RiskMap;
            """);

        migrationBuilder.DropColumn(name: "Role", table: "Users");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("THROW 51000, 'Converting integer domain IDs back to GUIDs is not supported.', 1;");
    }
}
