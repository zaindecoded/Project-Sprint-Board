using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskSprintBoard.Backend.Data.Migrations;

public partial class AlignTaskRiskSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            DECLARE @table sysname, @sql nvarchar(max), @objectId int;
            DECLARE legacyColumns CURSOR LOCAL FAST_FORWARD FOR
                SELECT name FROM (VALUES (N'Users'), (N'Projects'), (N'Sprints'), (N'Tasks'),
                    (N'Assignments'), (N'Comments'), (N'StatusHistory'), (N'TaskRiskEvaluations')) t(name);
            OPEN legacyColumns;
            FETCH NEXT FROM legacyColumns INTO @table;
            WHILE @@FETCH_STATUS = 0
            BEGIN
                SET @sql = N'';
                SET @objectId = OBJECT_ID(N'dbo.' + @table);
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=@objectId AND name=N'CreatedByLegacy')
                    SET @sql += N'ALTER TABLE dbo.' + QUOTENAME(@table) + N' DROP COLUMN CreatedByLegacy;';
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id=@objectId AND name=N'UpdatedByLegacy')
                    SET @sql += N'ALTER TABLE dbo.' + QUOTENAME(@table) + N' DROP COLUMN UpdatedByLegacy;';
                IF LEN(@sql) > 0 EXEC sys.sp_executesql @sql;
                FETCH NEXT FROM legacyColumns INTO @table;
            END;
            CLOSE legacyColumns;
            DEALLOCATE legacyColumns;

            IF COL_LENGTH(N'dbo.TaskRiskEvaluations', N'UserOverride') IS NULL
                ALTER TABLE dbo.TaskRiskEvaluations ADD UserOverride bit NULL;
            IF COL_LENGTH(N'dbo.TaskRiskEvaluations', N'OverrideReason') IS NULL
                ALTER TABLE dbo.TaskRiskEvaluations ADD OverrideReason nvarchar(max) NULL;
            IF COL_LENGTH(N'dbo.TaskRiskEvaluations', N'UpdatedAt') IS NULL
                ALTER TABLE dbo.TaskRiskEvaluations ADD UpdatedAt datetime2 NULL;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("THROW 51000, 'This schema alignment migration cannot restore removed legacy audit columns.', 1;");
    }
}
