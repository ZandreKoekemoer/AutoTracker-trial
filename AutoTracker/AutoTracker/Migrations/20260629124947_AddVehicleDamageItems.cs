using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleDamageItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerSignatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepairJobId = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomerName = table.Column<string>(type: "TEXT", nullable: false),
                    SignaturePath = table.Column<string>(type: "TEXT", nullable: false),
                    SignatureType = table.Column<string>(type: "TEXT", nullable: false),
                    SignedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSignatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerSignatures_RepairJobs_RepairJobId",
                        column: x => x.RepairJobId,
                        principalTable: "RepairJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobTimelineEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepairJobId = table.Column<int>(type: "INTEGER", nullable: false),
                    EntryType = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTimelineEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobTimelineEntries_RepairJobs_RepairJobId",
                        column: x => x.RepairJobId,
                        principalTable: "RepairJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VehicleDamageItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepairJobId = table.Column<int>(type: "INTEGER", nullable: false),
                    ViewSide = table.Column<string>(type: "TEXT", nullable: false),
                    BodyPart = table.Column<string>(type: "TEXT", nullable: false),
                    Condition = table.Column<string>(type: "TEXT", nullable: false),
                    RepairType = table.Column<string>(type: "TEXT", nullable: false),
                    LabourHours = table.Column<decimal>(type: "TEXT", nullable: false),
                    PaintHours = table.Column<decimal>(type: "TEXT", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleDamageItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleDamageItems_RepairJobs_RepairJobId",
                        column: x => x.RepairJobId,
                        principalTable: "RepairJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSignatures_RepairJobId",
                table: "CustomerSignatures",
                column: "RepairJobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTimelineEntries_RepairJobId",
                table: "JobTimelineEntries",
                column: "RepairJobId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDamageItems_RepairJobId",
                table: "VehicleDamageItems",
                column: "RepairJobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerSignatures");

            migrationBuilder.DropTable(
                name: "JobTimelineEntries");

            migrationBuilder.DropTable(
                name: "VehicleDamageItems");
        }
    }
}
