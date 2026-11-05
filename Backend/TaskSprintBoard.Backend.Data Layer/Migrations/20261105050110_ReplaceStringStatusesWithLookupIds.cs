using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TaskSprintBoard.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceStringStatusesWithLookupIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentStatusId",
                table: "TaskRiskEvaluations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriorityId",
                table: "TaskRiskEvaluations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NewStatusId",
                table: "StatusHistory",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OldStatusId",
                table: "StatusHistory",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusId",
                table: "Sprints",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusId",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoleInTaskId",
                table: "Assignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RoleInTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleInTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "status",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_status", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "RoleInTasks",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Reviewer" },
                    { 2, "Observer" },
                    { 3, "Assignee" }
                });

            migrationBuilder.InsertData(
                table: "status",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Planned" },
                    { 2, "Active" },
                    { 3, "OnHold" },
                    { 4, "Completed" },
                    { 5, "Cancelled" }
                });

            migrationBuilder.Sql(@"
UPDATE TaskRiskEvaluations SET
    PriorityId = CASE Priority WHEN 'Low' THEN 1 WHEN 'High' THEN 3 WHEN 'Critical' THEN 4 ELSE 2 END,
    CurrentStatusId = CASE CurrentStatus WHEN 'InProgress' THEN 2 WHEN 'Blocked' THEN 3 WHEN 'InReview' THEN 4 WHEN 'Done' THEN 5 WHEN 'Cancelled' THEN 6 ELSE 1 END;
UPDATE StatusHistory SET
    OldStatusId = CASE OldStatus WHEN 'InProgress' THEN 2 WHEN 'Blocked' THEN 3 WHEN 'InReview' THEN 4 WHEN 'Done' THEN 5 WHEN 'Cancelled' THEN 6 ELSE 1 END,
    NewStatusId = CASE NewStatus WHEN 'InProgress' THEN 2 WHEN 'Blocked' THEN 3 WHEN 'InReview' THEN 4 WHEN 'Done' THEN 5 WHEN 'Cancelled' THEN 6 ELSE 1 END;
UPDATE Projects SET StatusId = CASE Status WHEN 'Active' THEN 2 WHEN 'OnHold' THEN 3 WHEN 'Completed' THEN 4 WHEN 'Cancelled' THEN 5 ELSE 1 END;
UPDATE Sprints SET StatusId = CASE Status WHEN 'Active' THEN 2 WHEN 'OnHold' THEN 3 WHEN 'Completed' THEN 4 WHEN 'Cancelled' THEN 5 ELSE 1 END;
UPDATE Assignments SET RoleInTaskId = CASE RoleInTask WHEN 'Reviewer' THEN 1 WHEN 'Observer' THEN 2 ELSE 3 END;");

            foreach (var (column, table) in new[]
            {
                ("CurrentStatusId", "TaskRiskEvaluations"), ("PriorityId", "TaskRiskEvaluations"),
                ("NewStatusId", "StatusHistory"), ("OldStatusId", "StatusHistory"),
                ("StatusId", "Sprints"), ("StatusId", "Projects"), ("RoleInTaskId", "Assignments")
            })
                migrationBuilder.AlterColumn<int>(name: column, table: table, type: "int", nullable: false, oldClrType: typeof(int), oldType: "int", oldNullable: true);

            migrationBuilder.DropColumn(name: "CurrentStatus", table: "TaskRiskEvaluations");
            migrationBuilder.DropColumn(name: "Priority", table: "TaskRiskEvaluations");
            migrationBuilder.DropColumn(name: "NewStatus", table: "StatusHistory");
            migrationBuilder.DropColumn(name: "OldStatus", table: "StatusHistory");
            migrationBuilder.DropColumn(name: "Status", table: "Sprints");
            migrationBuilder.DropColumn(name: "Status", table: "Projects");
            migrationBuilder.DropColumn(name: "RoleInTask", table: "Assignments");

            migrationBuilder.CreateIndex(
                name: "IX_TaskRiskEvaluations_CurrentStatusId",
                table: "TaskRiskEvaluations",
                column: "CurrentStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskRiskEvaluations_PriorityId",
                table: "TaskRiskEvaluations",
                column: "PriorityId");

            migrationBuilder.CreateIndex(
                name: "IX_StatusHistory_NewStatusId",
                table: "StatusHistory",
                column: "NewStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_StatusHistory_OldStatusId",
                table: "StatusHistory",
                column: "OldStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_StatusId",
                table: "Sprints",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_StatusId",
                table: "Projects",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_RoleInTaskId",
                table: "Assignments",
                column: "RoleInTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleInTasks_Name",
                table: "RoleInTasks",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_status_Name",
                table: "status",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Assignments_RoleInTasks_RoleInTaskId",
                table: "Assignments",
                column: "RoleInTaskId",
                principalTable: "RoleInTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_status_StatusId",
                table: "Projects",
                column: "StatusId",
                principalTable: "status",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sprints_status_StatusId",
                table: "Sprints",
                column: "StatusId",
                principalTable: "status",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StatusHistory_TaskStatuses_NewStatusId",
                table: "StatusHistory",
                column: "NewStatusId",
                principalTable: "TaskStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StatusHistory_TaskStatuses_OldStatusId",
                table: "StatusHistory",
                column: "OldStatusId",
                principalTable: "TaskStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskRiskEvaluations_TaskPriorities_PriorityId",
                table: "TaskRiskEvaluations",
                column: "PriorityId",
                principalTable: "TaskPriorities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskRiskEvaluations_TaskStatuses_CurrentStatusId",
                table: "TaskRiskEvaluations",
                column: "CurrentStatusId",
                principalTable: "TaskStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assignments_RoleInTasks_RoleInTaskId",
                table: "Assignments");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_status_StatusId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Sprints_status_StatusId",
                table: "Sprints");

            migrationBuilder.DropForeignKey(
                name: "FK_StatusHistory_TaskStatuses_NewStatusId",
                table: "StatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_StatusHistory_TaskStatuses_OldStatusId",
                table: "StatusHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskRiskEvaluations_TaskPriorities_PriorityId",
                table: "TaskRiskEvaluations");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskRiskEvaluations_TaskStatuses_CurrentStatusId",
                table: "TaskRiskEvaluations");

            migrationBuilder.DropTable(
                name: "RoleInTasks");

            migrationBuilder.DropTable(
                name: "status");

            migrationBuilder.DropIndex(
                name: "IX_TaskRiskEvaluations_CurrentStatusId",
                table: "TaskRiskEvaluations");

            migrationBuilder.DropIndex(
                name: "IX_TaskRiskEvaluations_PriorityId",
                table: "TaskRiskEvaluations");

            migrationBuilder.DropIndex(
                name: "IX_StatusHistory_NewStatusId",
                table: "StatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_StatusHistory_OldStatusId",
                table: "StatusHistory");

            migrationBuilder.DropIndex(
                name: "IX_Sprints_StatusId",
                table: "Sprints");

            migrationBuilder.DropIndex(
                name: "IX_Projects_StatusId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Assignments_RoleInTaskId",
                table: "Assignments");

            migrationBuilder.DropColumn(
                name: "CurrentStatusId",
                table: "TaskRiskEvaluations");

            migrationBuilder.DropColumn(
                name: "PriorityId",
                table: "TaskRiskEvaluations");

            migrationBuilder.DropColumn(
                name: "NewStatusId",
                table: "StatusHistory");

            migrationBuilder.DropColumn(
                name: "OldStatusId",
                table: "StatusHistory");

            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "Sprints");

            migrationBuilder.DropColumn(
                name: "StatusId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RoleInTaskId",
                table: "Assignments");

            migrationBuilder.AddColumn<string>(
                name: "CurrentStatus",
                table: "TaskRiskEvaluations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "TaskRiskEvaluations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NewStatus",
                table: "StatusHistory",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OldStatus",
                table: "StatusHistory",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Sprints",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RoleInTask",
                table: "Assignments",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
