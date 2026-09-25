using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddJobCostProfitFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ActualConsumablesCost",
                table: "RepairJobs",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualLabourCost",
                table: "RepairJobs",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualPaintCost",
                table: "RepairJobs",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualPartsCost",
                table: "RepairJobs",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualSubletCost",
                table: "RepairJobs",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualTotalCost",
                table: "RepairJobs",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Profit",
                table: "RepairJobs",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ProfitMargin",
                table: "RepairJobs",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Parts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepairJobId = table.Column<int>(type: "INTEGER", nullable: false),
                    VehicleDamageItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    PartNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    QuotedUnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    ActualUnitCost = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalQuotedPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalActualCost = table.Column<decimal>(type: "TEXT", nullable: false),
                    Supplier = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CriticalPart = table.Column<bool>(type: "INTEGER", nullable: false),
                    InvoiceUploaded = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parts_RepairJobs_RepairJobId",
                        column: x => x.RepairJobId,
                        principalTable: "RepairJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Parts_VehicleDamageItems_VehicleDamageItemId",
                        column: x => x.VehicleDamageItemId,
                        principalTable: "VehicleDamageItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Parts_RepairJobId",
                table: "Parts",
                column: "RepairJobId");

            migrationBuilder.CreateIndex(
                name: "IX_Parts_VehicleDamageItemId",
                table: "Parts",
                column: "VehicleDamageItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Parts");

            migrationBuilder.DropColumn(
                name: "ActualConsumablesCost",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "ActualLabourCost",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "ActualPaintCost",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "ActualPartsCost",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "ActualSubletCost",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "ActualTotalCost",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "Profit",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "ProfitMargin",
                table: "RepairJobs");
        }
    }
}
