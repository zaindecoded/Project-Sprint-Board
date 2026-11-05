using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskSprintBoard.Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentUnassignedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UnassignedAt",
                table: "Assignments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnassignedAt",
                table: "Assignments");
        }
    }
}
