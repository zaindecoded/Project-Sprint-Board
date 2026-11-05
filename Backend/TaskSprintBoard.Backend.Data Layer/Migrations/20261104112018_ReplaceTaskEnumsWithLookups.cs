using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TaskSprintBoard.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceTaskEnumsWithLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskPriorities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskPriorities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskStatuses", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "TaskPriorities",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Low" },
                    { 2, "Medium" },
                    { 3, "High" },
                    { 4, "Critical" }
                });

            migrationBuilder.InsertData(
                table: "TaskStatuses",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Todo" },
                    { 2, "InProgress" },
                    { 3, "Blocked" },
                    { 4, "InReview" },
                    { 5, "Done" },
                    { 6, "Cancelled" }
                });

            migrationBuilder.AddColumn<int>("PriorityId", "Tasks", type: "int", nullable: false, defaultValue: 2);
            migrationBuilder.AddColumn<int>("StatusId", "Tasks", type: "int", nullable: false, defaultValue: 1);
            migrationBuilder.Sql("UPDATE Tasks SET PriorityId = CASE LTRIM(RTRIM(Priority)) WHEN 'Low' THEN 1 WHEN 'High' THEN 3 WHEN 'Critical' THEN 4 ELSE 2 END, StatusId = CASE LTRIM(RTRIM(Status)) WHEN 'InProgress' THEN 2 WHEN 'In Progress' THEN 2 WHEN 'Blocked' THEN 3 WHEN 'InReview' THEN 4 WHEN 'In Review' THEN 4 WHEN 'Done' THEN 5 WHEN 'Completed' THEN 5 WHEN 'Cancelled' THEN 6 ELSE 1 END");
            migrationBuilder.DropColumn("Priority", "Tasks");
            migrationBuilder.DropColumn("Status", "Tasks");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_PriorityId",
                table: "Tasks",
                column: "PriorityId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_StatusId",
                table: "Tasks",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskPriorities_Name",
                table: "TaskPriorities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskStatuses_Name",
                table: "TaskStatuses",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_TaskPriorities_PriorityId",
                table: "Tasks",
                column: "PriorityId",
                principalTable: "TaskPriorities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_TaskStatuses_StatusId",
                table: "Tasks",
                column: "StatusId",
                principalTable: "TaskStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>("Priority", "Tasks", type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Medium");
            migrationBuilder.AddColumn<string>("Status", "Tasks", type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Todo");
            migrationBuilder.Sql("UPDATE Tasks SET Priority = CASE PriorityId WHEN 1 THEN 'Low' WHEN 3 THEN 'High' WHEN 4 THEN 'Critical' ELSE 'Medium' END, Status = CASE StatusId WHEN 2 THEN 'InProgress' WHEN 3 THEN 'Blocked' WHEN 4 THEN 'InReview' WHEN 5 THEN 'Done' WHEN 6 THEN 'Cancelled' ELSE 'Todo' END");
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_TaskPriorities_PriorityId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_TaskStatuses_StatusId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_PriorityId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_StatusId",
                table: "Tasks");

            migrationBuilder.DropTable(name: "TaskPriorities");
            migrationBuilder.DropTable(name: "TaskStatuses");
            migrationBuilder.DropColumn("PriorityId", "Tasks");
            migrationBuilder.DropColumn("StatusId", "Tasks");
        }
    }
}
