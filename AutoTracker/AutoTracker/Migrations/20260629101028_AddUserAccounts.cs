using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RegNumber = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    VehicleType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Make = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ModelName = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    VinNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ClientId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehicles_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RepairJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClientId = table.Column<int>(type: "INTEGER", nullable: false),
                    VehicleId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    RfidTagCode = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairJobs_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairJobs_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Checklists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepairJobId = table.Column<int>(type: "INTEGER", nullable: false),
                    VehicleReceived = table.Column<bool>(type: "INTEGER", nullable: false),
                    DamagePhotosTaken = table.Column<bool>(type: "INTEGER", nullable: false),
                    AuthorizationConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PartsChecked = table.Column<bool>(type: "INTEGER", nullable: false),
                    RepairCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    QualityChecked = table.Column<bool>(type: "INTEGER", nullable: false),
                    FinalPhotosTaken = table.Column<bool>(type: "INTEGER", nullable: false),
                    ClientNotified = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Checklists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Checklists_RepairJobs_RepairJobId",
                        column: x => x.RepairJobId,
                        principalTable: "RepairJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepairJobId = table.Column<int>(type: "INTEGER", nullable: false),
                    DocumentType = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobDocuments_RepairJobs_RepairJobId",
                        column: x => x.RepairJobId,
                        principalTable: "RepairJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepairJobId = table.Column<int>(type: "INTEGER", nullable: false),
                    LabourDescription = table.Column<string>(type: "TEXT", nullable: false),
                    LabourAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    PartsDescription = table.Column<string>(type: "TEXT", nullable: false),
                    PartsAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    PaintDescription = table.Column<string>(type: "TEXT", nullable: false),
                    PaintAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    VatRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quotes_RepairJobs_RepairJobId",
                        column: x => x.RepairJobId,
                        principalTable: "RepairJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_RepairJobId",
                table: "Checklists",
                column: "RepairJobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobDocuments_RepairJobId",
                table: "JobDocuments",
                column: "RepairJobId");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_RepairJobId",
                table: "Quotes",
                column: "RepairJobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairJobs_ClientId",
                table: "RepairJobs",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairJobs_RfidTagCode",
                table: "RepairJobs",
                column: "RfidTagCode");

            migrationBuilder.CreateIndex(
                name: "IX_RepairJobs_VehicleId",
                table: "RepairJobs",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_ClientId",
                table: "Vehicles",
                column: "ClientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppUsers");

            migrationBuilder.DropTable(
                name: "Checklists");

            migrationBuilder.DropTable(
                name: "JobDocuments");

            migrationBuilder.DropTable(
                name: "Quotes");

            migrationBuilder.DropTable(
                name: "RepairJobs");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "Clients");
        }
    }
}
