using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoTracker.Migrations
{
    /// <inheritdoc />
    public partial class SyncRepairJobQuoteChecklistDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VatRate",
                table: "Quotes",
                newName: "Vat");

            migrationBuilder.RenameColumn(
                name: "PartsDescription",
                table: "Quotes",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "PartsAmount",
                table: "Quotes",
                newName: "Total");

            migrationBuilder.RenameColumn(
                name: "PaintDescription",
                table: "Quotes",
                newName: "Subtotal");

            migrationBuilder.RenameColumn(
                name: "PaintAmount",
                table: "Quotes",
                newName: "Sublet");

            migrationBuilder.RenameColumn(
                name: "LabourDescription",
                table: "Quotes",
                newName: "Parts");

            migrationBuilder.RenameColumn(
                name: "LabourAmount",
                table: "Quotes",
                newName: "Paint");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Quotes",
                newName: "Labour");

            migrationBuilder.AddColumn<string>(
                name: "AssignedTechnician",
                table: "RepairJobs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EstimatedCompletionDate",
                table: "RepairJobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternalNotes",
                table: "RepairJobs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "RepairJobs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "Quotes",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedTechnician",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "EstimatedCompletionDate",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "InternalNotes",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "Quotes");

            migrationBuilder.RenameColumn(
                name: "Vat",
                table: "Quotes",
                newName: "VatRate");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Quotes",
                newName: "PartsDescription");

            migrationBuilder.RenameColumn(
                name: "Total",
                table: "Quotes",
                newName: "PartsAmount");

            migrationBuilder.RenameColumn(
                name: "Subtotal",
                table: "Quotes",
                newName: "PaintDescription");

            migrationBuilder.RenameColumn(
                name: "Sublet",
                table: "Quotes",
                newName: "PaintAmount");

            migrationBuilder.RenameColumn(
                name: "Parts",
                table: "Quotes",
                newName: "LabourDescription");

            migrationBuilder.RenameColumn(
                name: "Paint",
                table: "Quotes",
                newName: "LabourAmount");

            migrationBuilder.RenameColumn(
                name: "Labour",
                table: "Quotes",
                newName: "CreatedAt");
        }
    }
}
