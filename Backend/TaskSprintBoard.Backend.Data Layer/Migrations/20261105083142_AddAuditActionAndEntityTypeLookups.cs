using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TaskSprintBoard.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditActionAndEntityTypeLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Action_EntityType_OccurredAt",
                table: "AuditLogs");

            migrationBuilder.CreateTable(
                name: "AuditActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditEntityTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntityTypes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "AuditActions",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Created" },
                    { 2, "Updated" },
                    { 3, "Deleted" },
                    { 4, "Status Changed" },
                    { 5, "Assigned" },
                    { 6, "Unassigned" },
                    { 7, "Assignment Updated" },
                    { 8, "Login" },
                    { 9, "Other" }
                });

            migrationBuilder.InsertData(
                table: "AuditEntityTypes",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Task" },
                    { 2, "Project" },
                    { 3, "Sprint" },
                    { 4, "User" },
                    { 5, "Assignment" },
                    { 6, "Comment" },
                    { 7, "TaskRiskEvaluation" },
                    { 8, "Role" },
                    { 9, "Status" },
                    { 10, "TaskPriorityLookup" },
                    { 11, "TaskStatusLookup" },
                    { 12, "RoleInTask" },
                    { 13, "Other" },
                    { 14, "Authentication" }
                });

            migrationBuilder.AddColumn<int>(name: "ActionId", table: "AuditLogs", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(name: "EntityTypeId", table: "AuditLogs", type: "int", nullable: true);

            migrationBuilder.Sql("""
                INSERT INTO AuditActions (Name)
                SELECT DISTINCT logs.Action FROM AuditLogs logs
                WHERE NOT EXISTS (SELECT 1 FROM AuditActions lookup WHERE lookup.Name = logs.Action);
                INSERT INTO AuditEntityTypes (Name)
                SELECT DISTINCT logs.EntityType FROM AuditLogs logs
                WHERE NOT EXISTS (SELECT 1 FROM AuditEntityTypes lookup WHERE lookup.Name = logs.EntityType);
                UPDATE logs SET ActionId = lookup.Id FROM AuditLogs logs JOIN AuditActions lookup ON lookup.Name = logs.Action;
                UPDATE logs SET EntityTypeId = lookup.Id FROM AuditLogs logs JOIN AuditEntityTypes lookup ON lookup.Name = logs.EntityType;
                """);

            migrationBuilder.AlterColumn<int>(name: "ActionId", table: "AuditLogs", type: "int", nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: true);
            migrationBuilder.AlterColumn<int>(name: "EntityTypeId", table: "AuditLogs", type: "int", nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActionId_EntityTypeId_OccurredAt",
                table: "AuditLogs",
                columns: new[] { "ActionId", "EntityTypeId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityTypeId",
                table: "AuditLogs",
                column: "EntityTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditActions_Name",
                table: "AuditActions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntityTypes_Name",
                table: "AuditEntityTypes",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_AuditActions_ActionId",
                table: "AuditLogs",
                column: "ActionId",
                principalTable: "AuditActions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_AuditEntityTypes_EntityTypeId",
                table: "AuditLogs",
                column: "EntityTypeId",
                principalTable: "AuditEntityTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(name: "Action", table: "AuditLogs");
            migrationBuilder.DropColumn(name: "EntityType", table: "AuditLogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "Action", table: "AuditLogs", type: "nvarchar(100)", maxLength: 100, nullable: true);
            migrationBuilder.AddColumn<string>(name: "EntityType", table: "AuditLogs", type: "nvarchar(100)", maxLength: 100, nullable: true);

            migrationBuilder.Sql("""
                UPDATE logs SET Action = lookup.Name FROM AuditLogs logs JOIN AuditActions lookup ON lookup.Id = logs.ActionId;
                UPDATE logs SET EntityType = lookup.Name FROM AuditLogs logs JOIN AuditEntityTypes lookup ON lookup.Id = logs.EntityTypeId;
                """);
            migrationBuilder.AlterColumn<string>(name: "Action", table: "AuditLogs", type: "nvarchar(100)", maxLength: 100, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(100)", oldMaxLength: 100, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "EntityType", table: "AuditLogs", type: "nvarchar(100)", maxLength: 100, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(100)", oldMaxLength: 100, oldNullable: true);

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_AuditActions_ActionId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_AuditEntityTypes_EntityTypeId",
                table: "AuditLogs");

            migrationBuilder.DropTable(
                name: "AuditActions");

            migrationBuilder.DropTable(
                name: "AuditEntityTypes");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_ActionId_EntityTypeId_OccurredAt",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_EntityTypeId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ActionId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "EntityTypeId",
                table: "AuditLogs");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action_EntityType_OccurredAt",
                table: "AuditLogs",
                columns: new[] { "Action", "EntityType", "OccurredAt" });
        }
    }
}
