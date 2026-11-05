using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskSprintBoard.Backend.Data.Migrations;

public partial class ConvertUserIdsToInt : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            SET XACT_ABORT ON;
            DECLARE @dropFks nvarchar(max) = N'';
            SELECT @dropFks += N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(parent_object_id)) + N' DROP CONSTRAINT ' + QUOTENAME(name) + N';'
            FROM sys.foreign_keys WHERE referenced_object_id = OBJECT_ID(N'dbo.Users');
            IF LEN(@dropFks) > 0 EXEC sys.sp_executesql @dropFks;

            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Assignments') AND name=N'IX_Assignments_TaskId_UserId') DROP INDEX IX_Assignments_TaskId_UserId ON dbo.Assignments;
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Assignments') AND name=N'IX_Assignments_UserId') DROP INDEX IX_Assignments_UserId ON dbo.Assignments;
            IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Comments') AND name=N'IX_Comments_UserId') DROP INDEX IX_Comments_UserId ON dbo.Comments;

            CREATE TABLE #UserMap (OldId uniqueidentifier NOT NULL PRIMARY KEY, NewId int IDENTITY(1,1) NOT NULL);
            INSERT INTO #UserMap (OldId) SELECT Id FROM dbo.Users ORDER BY Id;

            CREATE TABLE dbo.Users_Int (
                Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users_Int PRIMARY KEY,
                LegacyGuid uniqueidentifier NOT NULL, CreatedAt datetime2 NOT NULL,
                CreatedBy int NULL,
                UpdatedAt datetime2 NULL, UpdatedBy int NULL,
                IsActive bit NOT NULL, IsDeleted bit NOT NULL,
                FirstName nvarchar(100) NOT NULL, LastName nvarchar(100) NOT NULL,
                Email nvarchar(200) NOT NULL, UserName nvarchar(100) NOT NULL,
                PasswordHash nvarchar(max) NOT NULL, Role nvarchar(50) NOT NULL,
                RoleId int NOT NULL CONSTRAINT DF_Users_Int_RoleId DEFAULT 3
            );

            SET IDENTITY_INSERT dbo.Users_Int ON;
            INSERT INTO dbo.Users_Int (Id, LegacyGuid, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy,
                IsActive, IsDeleted, FirstName, LastName, Email, UserName, PasswordHash, Role, RoleId)
            SELECT m.NewId, u.Id, u.CreatedAt, createdActor.NewId, u.UpdatedAt, updatedActor.NewId,
                u.IsActive, u.IsDeleted, u.FirstName, u.LastName, u.Email, u.UserName, u.PasswordHash, u.Role, u.RoleId
            FROM dbo.Users u JOIN #UserMap m ON m.OldId = u.Id
            OUTER APPLY (SELECT TOP (1) actorMap.NewId FROM dbo.Users actor JOIN #UserMap actorMap ON actorMap.OldId = actor.Id
                WHERE LTRIM(RTRIM(u.CreatedBy)) IN (LTRIM(RTRIM(actor.UserName)), LTRIM(RTRIM(actor.Email)))) createdActor
            OUTER APPLY (SELECT TOP (1) actorMap.NewId FROM dbo.Users actor JOIN #UserMap actorMap ON actorMap.OldId = actor.Id
                WHERE LTRIM(RTRIM(u.UpdatedBy)) IN (LTRIM(RTRIM(actor.UserName)), LTRIM(RTRIM(actor.Email)))) updatedActor;
            SET IDENTITY_INSERT dbo.Users_Int OFF;

            DECLARE @auditTable sysname;
            DECLARE auditTables CURSOR LOCAL FAST_FORWARD FOR
                SELECT name FROM (VALUES (N'Projects'), (N'Sprints'), (N'Tasks'), (N'Assignments'), (N'Comments'), (N'StatusHistory'), (N'TaskRiskEvaluations')) v(name);
            OPEN auditTables;
            FETCH NEXT FROM auditTables INTO @auditTable;
            WHILE @@FETCH_STATUS = 0
            BEGIN
                    DECLARE @addAuditColumns nvarchar(max) = N'ALTER TABLE dbo.' + QUOTENAME(@auditTable) + N' ADD CreatedByInt int NULL, UpdatedByInt int NULL;';
                    EXEC sys.sp_executesql @addAuditColumns;
                DECLARE @auditSql nvarchar(max) = N'
                        UPDATE target SET CreatedByInt = createdActor.NewId,
                            UpdatedByInt = updatedActor.NewId
                    FROM dbo.' + QUOTENAME(@auditTable) + N' target
                    OUTER APPLY (SELECT TOP (1) m.NewId FROM dbo.Users actor JOIN #UserMap m ON m.OldId = actor.Id WHERE LTRIM(RTRIM(target.CreatedBy)) IN (LTRIM(RTRIM(actor.UserName)), LTRIM(RTRIM(actor.Email)))) createdActor
                    OUTER APPLY (SELECT TOP (1) m.NewId FROM dbo.Users actor JOIN #UserMap m ON m.OldId = actor.Id WHERE LTRIM(RTRIM(target.UpdatedBy)) IN (LTRIM(RTRIM(actor.UserName)), LTRIM(RTRIM(actor.Email)))) updatedActor;
                    ALTER TABLE dbo.' + QUOTENAME(@auditTable) + N' DROP COLUMN CreatedBy, UpdatedBy;
                    EXEC sys.sp_rename N''dbo.' + @auditTable + N'.CreatedByInt'', N''CreatedBy'', N''COLUMN'';
                    EXEC sys.sp_rename N''dbo.' + @auditTable + N'.UpdatedByInt'', N''UpdatedBy'', N''COLUMN'';';
                EXEC sys.sp_executesql @auditSql;
                FETCH NEXT FROM auditTables INTO @auditTable;
            END;
            CLOSE auditTables;
            DEALLOCATE auditTables;

            ALTER TABLE dbo.Assignments ADD UserIdInt int NULL;
            EXEC sys.sp_executesql N'
                UPDATE a SET UserIdInt = m.NewId FROM dbo.Assignments a JOIN #UserMap m ON m.OldId = a.UserId;
                IF EXISTS (SELECT 1 FROM dbo.Assignments WHERE UserIdInt IS NULL) THROW 51001, ''Could not map an assignment to a user.'', 1;';
            ALTER TABLE dbo.Assignments DROP COLUMN UserId;
            EXEC sys.sp_rename N'dbo.Assignments.UserIdInt', N'UserId', N'COLUMN';
            ALTER TABLE dbo.Assignments ALTER COLUMN UserId int NOT NULL;

            ALTER TABLE dbo.Comments ADD UserIdInt int NULL;
            EXEC sys.sp_executesql N'
                UPDATE c SET UserIdInt = m.NewId FROM dbo.Comments c JOIN #UserMap m ON m.OldId = c.UserId;
                IF EXISTS (SELECT 1 FROM dbo.Comments WHERE UserIdInt IS NULL) THROW 51002, ''Could not map a comment to a user.'', 1;';
            ALTER TABLE dbo.Comments DROP COLUMN UserId;
            EXEC sys.sp_rename N'dbo.Comments.UserIdInt', N'UserId', N'COLUMN';
            ALTER TABLE dbo.Comments ALTER COLUMN UserId int NOT NULL;

            DROP TABLE dbo.Users;
            EXEC sys.sp_rename N'dbo.Users_Int', N'Users';
            EXEC sys.sp_rename N'dbo.Users.LegacyGuid', N'LegacyGuidToDrop', N'COLUMN';
            ALTER TABLE dbo.Users DROP COLUMN LegacyGuidToDrop;
            CREATE UNIQUE INDEX IX_Users_Email ON dbo.Users(Email);
            CREATE UNIQUE INDEX IX_Users_UserName ON dbo.Users(UserName);
            CREATE INDEX IX_Users_RoleId ON dbo.Users(RoleId);
            ALTER TABLE dbo.Users ADD CONSTRAINT FK_Users_Roles_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id) ON DELETE NO ACTION;
            CREATE INDEX IX_Assignments_UserId ON dbo.Assignments(UserId);
            CREATE UNIQUE INDEX IX_Assignments_TaskId_UserId ON dbo.Assignments(TaskId, UserId);
            CREATE INDEX IX_Comments_UserId ON dbo.Comments(UserId);
            ALTER TABLE dbo.Assignments ADD CONSTRAINT FK_Assignments_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE;
            ALTER TABLE dbo.Comments ADD CONSTRAINT FK_Comments_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION;
            DROP TABLE #UserMap;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("THROW 51000, 'Converting integer user IDs back to GUIDs is not supported.', 1;");
    }
}
