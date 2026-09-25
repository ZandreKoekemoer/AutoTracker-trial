using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoTracker.Migrations
{
    /// <inheritdoc />
    public partial class StableTechnicianAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedTechnicianUserId",
                table: "RepairJobs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairJobs_AssignedTechnicianUserId",
                table: "RepairJobs",
                column: "AssignedTechnicianUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RepairJobs_AssignedTechnicianUserId",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "AssignedTechnicianUserId",
                table: "RepairJobs");
        }
    }
}
