using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoTracker.Migrations
{
    /// <inheritdoc />
    public partial class SecureClientAccessAndSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RfidTags_CurrentRepairJobId",
                table: "RfidTags");

            migrationBuilder.AddColumn<string>(
                name: "TrackingToken",
                table: "RepairJobs",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsClientVisible",
                table: "JobTimelineEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsClientVisible",
                table: "JobPhotos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "CostAmount",
                table: "JobDocuments",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CostCategory",
                table: "JobDocuments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "ManualCostOverride",
                table: "JobDocuments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "AppUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAtUtc",
                table: "AppUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SessionVersion",
                table: "AppUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ClientTrackingAccesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepairJobId = table.Column<int>(type: "INTEGER", nullable: false),
                    TokenHash = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    LastAccessedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientTrackingAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientTrackingAccesses_RepairJobs_RepairJobId",
                        column: x => x.RepairJobId,
                        principalTable: "RepairJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSecurityAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActorUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSecurityAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RfidTags_CurrentRepairJobId",
                table: "RfidTags",
                column: "CurrentRepairJobId",
                unique: true,
                filter: "CurrentRepairJobId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RfidTags_TagCode",
                table: "RfidTags",
                column: "TagCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_CompanyId",
                table: "AppUsers",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientTrackingAccesses_RepairJobId",
                table: "ClientTrackingAccesses",
                column: "RepairJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientTrackingAccesses_TokenHash",
                table: "ClientTrackingAccesses",
                column: "TokenHash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AppUsers_Companies_CompanyId",
                table: "AppUsers",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppUsers_Companies_CompanyId",
                table: "AppUsers");

            migrationBuilder.DropTable(
                name: "ClientTrackingAccesses");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "UserSecurityAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_RfidTags_CurrentRepairJobId",
                table: "RfidTags");

            migrationBuilder.DropIndex(
                name: "IX_RfidTags_TagCode",
                table: "RfidTags");

            migrationBuilder.DropIndex(
                name: "IX_AppUsers_CompanyId",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "TrackingToken",
                table: "RepairJobs");

            migrationBuilder.DropColumn(
                name: "IsClientVisible",
                table: "JobTimelineEntries");

            migrationBuilder.DropColumn(
                name: "IsClientVisible",
                table: "JobPhotos");

            migrationBuilder.DropColumn(
                name: "CostAmount",
                table: "JobDocuments");

            migrationBuilder.DropColumn(
                name: "CostCategory",
                table: "JobDocuments");

            migrationBuilder.DropColumn(
                name: "ManualCostOverride",
                table: "JobDocuments");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "LastLoginAtUtc",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "SessionVersion",
                table: "AppUsers");

            migrationBuilder.CreateIndex(
                name: "IX_RfidTags_CurrentRepairJobId",
                table: "RfidTags",
                column: "CurrentRepairJobId");
        }
    }
}
