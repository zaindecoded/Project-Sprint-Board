using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskSprintBoard.Backend.Data.Migrations
{
    public partial class AddRoleLookupAndUserRoleId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF COL_LENGTH(N'dbo.Users', N'RoleId') IS NULL
                    ALTER TABLE dbo.Users ADD RoleId int NOT NULL CONSTRAINT DF_Users_RoleId DEFAULT 3;

                IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.Roles (
                        Id int NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
                        Name nvarchar(50) NOT NULL
                    );
                    CREATE UNIQUE INDEX IX_Roles_Name ON dbo.Roles(Name);
                END;

                IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Id = 1)
                    INSERT INTO dbo.Roles (Id, Name) VALUES (1, N'Admin');
                ELSE UPDATE dbo.Roles SET Name = N'Admin' WHERE Id = 1;

                IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Id = 2)
                    INSERT INTO dbo.Roles (Id, Name) VALUES (2, N'Project Manager');
                ELSE UPDATE dbo.Roles SET Name = N'Project Manager' WHERE Id = 2;

                IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Id = 3)
                    INSERT INTO dbo.Roles (Id, Name) VALUES (3, N'Project Member');
                ELSE UPDATE dbo.Roles SET Name = N'Project Member' WHERE Id = 3;

                UPDATE dbo.Users
                SET RoleId = CASE
                    WHEN Role IN (N'Admin', N'Administrator') THEN 1
                    WHEN Role IN (N'ProjectManager', N'Project Manager') THEN 2
                    WHEN Role IN (N'TeamMember', N'ProjectMember', N'Project Member') THEN 3
                    ELSE 3
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = N'IX_Users_RoleId')
                    CREATE INDEX IX_Users_RoleId ON dbo.Users(RoleId);

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Users_Roles_RoleId')
                    ALTER TABLE dbo.Users ADD CONSTRAINT FK_Users_Roles_RoleId
                        FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id) ON DELETE NO ACTION;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Users_Roles_RoleId')
                    ALTER TABLE dbo.Users DROP CONSTRAINT FK_Users_Roles_RoleId;
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Users') AND name = N'IX_Users_RoleId')
                    DROP INDEX IX_Users_RoleId ON dbo.Users;
                IF COL_LENGTH(N'dbo.Users', N'RoleId') IS NOT NULL ALTER TABLE dbo.Users DROP COLUMN RoleId;
                IF OBJECT_ID(N'dbo.Roles', N'U') IS NOT NULL DROP TABLE dbo.Roles;
            ");
        }
    }
}
