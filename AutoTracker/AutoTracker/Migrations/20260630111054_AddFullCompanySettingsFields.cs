using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddFullCompanySettingsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CollectedAt",
                table: "RepairJobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CollectionCustomerName",
                table: "RepairJobs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CollectionSignaturePath",
                table: "RepairJobs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "InvoiceDate",
                table: "Quotes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "Quotes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BankingDetails",
                table: "CompanySettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CKNumber",
                table: "CompanySettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ChecklistFooterText",
                table: "CompanySettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompanySignatureName",
                table: "CompanySettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompanySignaturePath",
                table: "CompanySettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultConsumables",
                table: "CompanySettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultFreight",
                table: "CompanySettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultSundries",
                table: "CompanySettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultWasteDisposal",
                table: "CompanySettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "InvoicePrefix",
                table: "CompanySettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "LabourRatePerHour",
                table: "CompanySettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "LastInvoiceNumber",
                table: "CompanySettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LastQuoteNumber",
                table: "CompanySettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MechanicalRatePerHour",
                table: "CompanySettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PaintRatePerPanel",
                table: "CompanySettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PanelBeatingRatePerHour",
                table: "CompanySettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "QuotePrefix",
                table: "CompanySettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "CompanySettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "StripAssembleRatePerHour",
                table: "CompanySettings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ThemeMode",
                table: "CompanySettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "CompanySettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "CalendarEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RepairJobId = table.Column<int>(type: "INTEGER", nullable: true),
                    EventType = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarEvents_RepairJobs_RepairJobId",
                        column: x => x.RepairJobId,
                        principalTable: "RepairJobs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QuoteLineItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepairJobId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuoteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Section = table.Column<string>(type: "TEXT", nullable: false),
                    Method = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    Hours = table.Column<decimal>(type: "TEXT", nullable: false),
                    Panels = table.Column<decimal>(type: "TEXT", nullable: false),
                    Value = table.Column<decimal>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuoteLineItems_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuoteLineItems_RepairJobs_RepairJobId",
                        column: x => x.RepairJobId,
                        principalTable: "RepairJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_RepairJobId",
                table: "CalendarEvents",
                column: "RepairJobId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteLineItems_QuoteId",
                table: "QuoteLineItems",
                column: "QuoteId");

            migrationBuilder.CreateIndex(
                name: "IX_QuoteLineItems_RepairJobId",
                table: "QuoteLineItems",
                column: "RepairJobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarEvents");

            migrationBuilder.DropTable(
                name: "QuoteLineItems");

            migrationBuilder.DropColumn(
                name: "CollectedAt",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "CollectionCustomerName",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "CollectionSignaturePath",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "InvoiceDate",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "BankingDetails",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "CKNumber",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ChecklistFooterText",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "CompanySignatureName",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "CompanySignaturePath",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "DefaultConsumables",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "DefaultFreight",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "DefaultSundries",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "DefaultWasteDisposal",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "InvoicePrefix",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "LabourRatePerHour",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "LastInvoiceNumber",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "LastQuoteNumber",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "MechanicalRatePerHour",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PaintRatePerPanel",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "PanelBeatingRatePerHour",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "QuotePrefix",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "RegistrationNumber",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "StripAssembleRatePerHour",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "ThemeMode",
                table: "CompanySettings");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "CompanySettings");
        }
    }
}
