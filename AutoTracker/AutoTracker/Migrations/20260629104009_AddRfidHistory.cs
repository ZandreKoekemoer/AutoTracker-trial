using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddRfidHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RfidTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TagCode = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentRepairJobId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RfidTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RfidTags_RepairJobs_CurrentRepairJobId",
                        column: x => x.CurrentRepairJobId,
                        principalTable: "RepairJobs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RfidTagHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RfidTagId = table.Column<int>(type: "INTEGER", nullable: false),
                    RepairJobId = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RfidTagHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RfidTagHistories_RepairJobs_RepairJobId",
                        column: x => x.RepairJobId,
                        principalTable: "RepairJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RfidTagHistories_RfidTags_RfidTagId",
                        column: x => x.RfidTagId,
                        principalTable: "RfidTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RfidTagHistories_RepairJobId",
                table: "RfidTagHistories",
                column: "RepairJobId");

            migrationBuilder.CreateIndex(
                name: "IX_RfidTagHistories_RfidTagId",
                table: "RfidTagHistories",
                column: "RfidTagId");

            migrationBuilder.CreateIndex(
                name: "IX_RfidTags_CurrentRepairJobId",
                table: "RfidTags",
                column: "CurrentRepairJobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RfidTagHistories");

            migrationBuilder.DropTable(
                name: "RfidTags");
        }
    }
}
