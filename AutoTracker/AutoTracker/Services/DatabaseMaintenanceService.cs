using AutoTracker.Data;
using AutoTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoTracker.Services;

public class DatabaseMaintenanceService
{
    private readonly AppDbContext _db;
    public DatabaseMaintenanceService(AppDbContext db) => _db = db;

    public void EnsureUpdated()
    {
        _db.Database.EnsureCreated();
        AddCompanyColumns();
        AddQuoteColumns();
        AddRepairColumns();
        CreateQuoteLineItems();
        CreateCalendarEvents();
        CreateCompanies();
        AddSecurityAndFinanceColumns();
        AddSecureAccessSchema();
        EnsureAppUserCompanyForeignKey();
        MigrateLegacyTrackingTokens();
        MigrateRolesToNewSystem();
        ReconcileMigrationHistory();
    }

    private void AddColumn(string table, string column, string definition)
    {
        var connection = _db.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose) connection.Open();
        try
        {
            using var check = connection.CreateCommand();
            check.CommandText = $"PRAGMA table_info(\"{table}\")";
            using var reader = check.ExecuteReader();
            var exists = false;
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }
            reader.Close();
            if (exists) return;
            using var alter = connection.CreateCommand();
            alter.CommandText = $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {definition}";
            alter.ExecuteNonQuery();
        }
        finally
        {
            if (shouldClose) connection.Close();
        }
    }

    private void AddCompanyColumns()
    {
        AddColumn("CompanySettings", "Website", "TEXT NOT NULL DEFAULT ''");
        AddColumn("CompanySettings", "RegistrationNumber", "TEXT NOT NULL DEFAULT ''");
        AddColumn("CompanySettings", "CKNumber", "TEXT NOT NULL DEFAULT ''");
        AddColumn("CompanySettings", "LabourRatePerHour", "TEXT NOT NULL DEFAULT '0'");
        AddColumn("CompanySettings", "PaintRatePerPanel", "TEXT NOT NULL DEFAULT '0'");
        AddColumn("CompanySettings", "StripAssembleRatePerHour", "TEXT NOT NULL DEFAULT '0'");
        AddColumn("CompanySettings", "MechanicalRatePerHour", "TEXT NOT NULL DEFAULT '0'");
        AddColumn("CompanySettings", "PanelBeatingRatePerHour", "TEXT NOT NULL DEFAULT '0'");
        AddColumn("CompanySettings", "DefaultSundries", "TEXT NOT NULL DEFAULT '0'");
        AddColumn("CompanySettings", "DefaultConsumables", "TEXT NOT NULL DEFAULT '0'");
        AddColumn("CompanySettings", "DefaultFreight", "TEXT NOT NULL DEFAULT '0'");
        AddColumn("CompanySettings", "DefaultWasteDisposal", "TEXT NOT NULL DEFAULT '0'");
        AddColumn("CompanySettings", "QuotePrefix", "TEXT NOT NULL DEFAULT 'EST'");
        AddColumn("CompanySettings", "LastQuoteNumber", "INTEGER NOT NULL DEFAULT 0");
        AddColumn("CompanySettings", "InvoicePrefix", "TEXT NOT NULL DEFAULT 'INV'");
        AddColumn("CompanySettings", "LastInvoiceNumber", "INTEGER NOT NULL DEFAULT 0");
        AddColumn("CompanySettings", "ChecklistFooterText", "TEXT NOT NULL DEFAULT ''");
        AddColumn("CompanySettings", "BankingDetails", "TEXT NOT NULL DEFAULT ''");
        AddColumn("CompanySettings", "CompanySignatureName", "TEXT NOT NULL DEFAULT ''");
        AddColumn("CompanySettings", "CompanySignaturePath", "TEXT NOT NULL DEFAULT ''");
        AddColumn("CompanySettings", "ThemeMode", "TEXT NOT NULL DEFAULT 'dark'");
    }

    private void AddQuoteColumns()
    {
        AddColumn("Quotes", "InvoiceNumber", "TEXT NOT NULL DEFAULT ''");
        AddColumn("Quotes", "InvoiceDate", "TEXT NULL");
    }

    private void AddRepairColumns()
    {
        AddColumn("RepairJobs", "CollectionCustomerName", "TEXT NOT NULL DEFAULT ''");
        AddColumn("RepairJobs", "CollectionSignaturePath", "TEXT NOT NULL DEFAULT ''");
        AddColumn("RepairJobs", "CollectedAt", "TEXT NULL");
    }

    private void CreateCompanies()
    {
        _db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS Companies (
            Id INTEGER NOT NULL CONSTRAINT PK_Companies PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL DEFAULT 'Company',
            IsActive INTEGER NOT NULL DEFAULT 1,
            CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
        )");
        var count = _db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM Companies").AsEnumerable().FirstOrDefault();
        if (count == 0) _db.Database.ExecuteSqlRaw("INSERT INTO Companies (Name, IsActive, CreatedAt) VALUES ('Default Company', 1, CURRENT_TIMESTAMP)");
    }

    private void AddSecurityAndFinanceColumns()
    {
        AddColumn("AppUsers", "CompanyId", "INTEGER NOT NULL DEFAULT 1");
        AddColumn("RepairJobs", "TrackingToken", "TEXT NOT NULL DEFAULT ''");
        AddColumn("JobDocuments", "CostAmount", "TEXT NOT NULL DEFAULT '0'");
        AddColumn("JobDocuments", "CostCategory", "TEXT NOT NULL DEFAULT ''");
        AddColumn("JobDocuments", "ManualCostOverride", "INTEGER NOT NULL DEFAULT 0");
        AddColumn("RepairJobs", "ActualPartsCost", "TEXT NOT NULL DEFAULT '0'");
        AddColumn("RepairJobs", "ActualLabourCost", "TEXT NOT NULL DEFAULT '0'");
        AddColumn("RepairJobs", "ActualPaintCost", "TEXT NOT NULL DEFAULT '0'");
        AddColumn("RepairJobs", "ActualConsumablesCost", "TEXT NOT NULL DEFAULT '0'");
        AddColumn("RepairJobs", "ActualSubletCost", "TEXT NOT NULL DEFAULT '0'");
        AddColumn("RepairJobs", "ActualTotalCost", "TEXT NOT NULL DEFAULT '0'");
        AddColumn("RepairJobs", "Profit", "TEXT NOT NULL DEFAULT '0'");
        AddColumn("RepairJobs", "ProfitMargin", "TEXT NOT NULL DEFAULT '0'");
    }

    private void AddSecureAccessSchema()
    {
        AddColumn("AppUsers", "SessionVersion", "INTEGER NOT NULL DEFAULT 1");
        AddColumn("AppUsers", "LastLoginAtUtc", "TEXT NULL");
        AddColumn("RepairJobs", "AssignedTechnicianUserId", "INTEGER NULL");
        AddColumn("JobPhotos", "IsClientVisible", "INTEGER NOT NULL DEFAULT 0");
        AddColumn("JobTimelineEntries", "IsClientVisible", "INTEGER NOT NULL DEFAULT 0");

        _db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS ClientTrackingAccesses (
            Id INTEGER NOT NULL CONSTRAINT PK_ClientTrackingAccesses PRIMARY KEY AUTOINCREMENT,
            RepairJobId INTEGER NOT NULL,
            TokenHash TEXT NOT NULL,
            ExpiresAtUtc TEXT NOT NULL,
            RevokedAtUtc TEXT NULL,
            CreatedAtUtc TEXT NOT NULL,
            CreatedByUserId INTEGER NOT NULL,
            LastAccessedAtUtc TEXT NULL,
            CONSTRAINT FK_ClientTrackingAccesses_RepairJobs_RepairJobId FOREIGN KEY (RepairJobId) REFERENCES RepairJobs (Id) ON DELETE CASCADE
        )");
        _db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_ClientTrackingAccesses_TokenHash ON ClientTrackingAccesses (TokenHash)");
        _db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_ClientTrackingAccesses_RepairJobId ON ClientTrackingAccesses (RepairJobId)");
        _db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_RepairJobs_AssignedTechnicianUserId ON RepairJobs (AssignedTechnicianUserId)");
        _db.Database.ExecuteSqlRaw(@"UPDATE RepairJobs
SET AssignedTechnicianUserId = (
    SELECT MIN(u.Id) FROM AppUsers u
    WHERE u.IsActive = 1 AND u.Role = 'Technician' AND u.FullName = RepairJobs.AssignedTechnician
)
WHERE AssignedTechnicianUserId IS NULL
  AND AssignedTechnician <> ''
  AND 1 = (SELECT COUNT(*) FROM AppUsers u WHERE u.IsActive = 1 AND u.Role = 'Technician' AND u.FullName = RepairJobs.AssignedTechnician);");

        _db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS UserSecurityAuditLogs (
            Id INTEGER NOT NULL CONSTRAINT PK_UserSecurityAuditLogs PRIMARY KEY AUTOINCREMENT,
            ActorUserId INTEGER NOT NULL,
            TargetUserId INTEGER NOT NULL,
            Action TEXT NOT NULL,
            CreatedAtUtc TEXT NOT NULL
        )");
        _db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_UserSecurityAuditLogs_TargetUserId ON UserSecurityAuditLogs (TargetUserId)");

        _db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_RfidTags_TagCode ON RfidTags (TagCode)");
        _db.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_RfidTags_CurrentRepairJobId ON RfidTags (CurrentRepairJobId) WHERE CurrentRepairJobId IS NOT NULL");
    }

    private void EnsureAppUserCompanyForeignKey()
    {
        var connection = _db.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose) connection.Open();
        try
        {
            using (var check = connection.CreateCommand())
            {
                check.CommandText = "PRAGMA foreign_key_list(\"AppUsers\")";
                using var reader = check.ExecuteReader();
                while (reader.Read())
                {
                    if (string.Equals(reader.GetString(2), "Companies", StringComparison.OrdinalIgnoreCase))
                        return;
                }
            }

            _db.ChangeTracker.Clear();
            Execute(connection, "PRAGMA foreign_keys = OFF;");
            Execute(connection, "BEGIN IMMEDIATE;");
            try
            {
                Execute(connection, "DROP TABLE IF EXISTS AppUsers_rebuild;");
                Execute(connection, @"CREATE TABLE AppUsers_rebuild (
                    Id INTEGER NOT NULL CONSTRAINT PK_AppUsers PRIMARY KEY AUTOINCREMENT,
                    FullName TEXT NOT NULL,
                    Email TEXT NOT NULL,
                    PasswordHash TEXT NOT NULL,
                    Role TEXT NOT NULL,
                    IsActive INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    CompanyId INTEGER NOT NULL DEFAULT 1,
                    SessionVersion INTEGER NOT NULL DEFAULT 1,
                    LastLoginAtUtc TEXT NULL,
                    CONSTRAINT FK_AppUsers_Companies_CompanyId FOREIGN KEY (CompanyId) REFERENCES Companies (Id) ON DELETE CASCADE
                );");
                Execute(connection, @"INSERT INTO AppUsers_rebuild
                    (Id, FullName, Email, PasswordHash, Role, IsActive, CreatedAt, CompanyId, SessionVersion, LastLoginAtUtc)
                    SELECT Id, FullName, Email, PasswordHash, Role, IsActive, CreatedAt, CompanyId, SessionVersion, LastLoginAtUtc FROM AppUsers;");
                Execute(connection, "DROP TABLE AppUsers;");
                Execute(connection, "ALTER TABLE AppUsers_rebuild RENAME TO AppUsers;");
                Execute(connection, "CREATE UNIQUE INDEX IF NOT EXISTS IX_AppUsers_Email ON AppUsers (Email);");
                Execute(connection, "CREATE INDEX IF NOT EXISTS IX_AppUsers_CompanyId ON AppUsers (CompanyId);");
                Execute(connection, "DELETE FROM sqlite_sequence WHERE name IN ('AppUsers', 'AppUsers_rebuild');");
                Execute(connection, "INSERT INTO sqlite_sequence(name, seq) SELECT 'AppUsers', COALESCE(MAX(Id), 0) FROM AppUsers;");
                Execute(connection, "COMMIT;");
            }
            catch
            {
                Execute(connection, "ROLLBACK;");
                throw;
            }
            finally
            {
                Execute(connection, "PRAGMA foreign_keys = ON;");
            }
        }
        finally
        {
            if (shouldClose) connection.Close();
        }
    }

    private static void Execute(System.Data.Common.DbConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private void MigrateLegacyTrackingTokens()
    {
        var legacyJobs = _db.RepairJobs
            .Where(j => j.TrackingToken != null && j.TrackingToken != "")
            .ToList();
        if (legacyJobs.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var job in legacyJobs)
        {
            var hash = ClientTrackingService.HashToken(job.TrackingToken);
            if (!_db.ClientTrackingAccesses.Any(x => x.TokenHash == hash))
            {
                _db.ClientTrackingAccesses.Add(new ClientTrackingAccess
                {
                    RepairJobId = job.Id,
                    TokenHash = hash,
                    ExpiresAtUtc = now.AddDays(30),
                    CreatedAtUtc = now,
                    CreatedByUserId = 0
                });
            }
            job.TrackingToken = "";
        }
        _db.SaveChanges();
    }

    private void ReconcileMigrationHistory()
    {
        _db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
            MigrationId TEXT NOT NULL CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY,
            ProductVersion TEXT NOT NULL
        )");
        var migrationIds = new[]
        {
            "20260629101028_AddUserAccounts",
            "20260629101459_AddCompanySettings",
            "20260629103138_SyncRepairJobQuoteChecklistDocuments",
            "20260629104009_AddRfidHistory",
            "20260629112914_AddAuditLog",
            "20260629115124_AddJobPhotoGallery",
            "20260629124947_AddVehicleDamageItems",
            "20260629155444_AddJobCostProfitFields",
            "20260630111054_AddFullCompanySettingsFields",
            "20260816000000_RenameRolesToNewSystem",
            "20260909222849_SecureClientAccessAndSessions",
            "20260909231856_StableTechnicianAssignments"
        };
        foreach (var migrationId in migrationIds)
            _db.Database.ExecuteSqlInterpolated($"INSERT OR IGNORE INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ({migrationId}, {"8.0.8"})");
    }

    private void CreateQuoteLineItems()
    {
        _db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS QuoteLineItems (
            Id INTEGER NOT NULL CONSTRAINT PK_QuoteLineItems PRIMARY KEY AUTOINCREMENT,
            RepairJobId INTEGER NOT NULL,
            QuoteId INTEGER NOT NULL,
            Section TEXT NOT NULL DEFAULT '',
            Method TEXT NOT NULL DEFAULT '',
            Description TEXT NOT NULL DEFAULT '',
            Code TEXT NOT NULL DEFAULT '',
            Quantity TEXT NOT NULL DEFAULT '0',
            UnitPrice TEXT NOT NULL DEFAULT '0',
            Hours TEXT NOT NULL DEFAULT '0',
            Panels TEXT NOT NULL DEFAULT '0',
            Value TEXT NOT NULL DEFAULT '0',
            SortOrder INTEGER NOT NULL DEFAULT 0
        )");
    }

    private void MigrateRolesToNewSystem()
    {
        // Owner / Admin → Administrator
        _db.Database.ExecuteSqlRaw("UPDATE AppUsers SET Role = 'Administrator' WHERE Role IN ('Owner', 'Admin')");
        // Manager → Workshop Manager
        _db.Database.ExecuteSqlRaw("UPDATE AppUsers SET Role = 'Workshop Manager' WHERE Role = 'Manager'");
        // Workshop User / Estimator / Read Only → Technician
        _db.Database.ExecuteSqlRaw("UPDATE AppUsers SET Role = 'Technician' WHERE Role IN ('Workshop User', 'Estimator', 'Read Only')");
    }

    private void CreateCalendarEvents()
    {
        _db.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS CalendarEvents (
            Id INTEGER NOT NULL CONSTRAINT PK_CalendarEvents PRIMARY KEY AUTOINCREMENT,
            Title TEXT NOT NULL DEFAULT '',
            Description TEXT NOT NULL DEFAULT '',
            StartTime TEXT NOT NULL,
            EndTime TEXT NULL,
            RepairJobId INTEGER NULL,
            EventType TEXT NOT NULL DEFAULT 'Manual',
            CreatedAt TEXT NOT NULL
        )");
    }
}
