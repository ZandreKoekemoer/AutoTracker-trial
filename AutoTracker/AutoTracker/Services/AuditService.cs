using AutoTracker.Data;
using AutoTracker.Models;

namespace AutoTracker.Services;

public class AuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(int repairJobId, string action, string userName = "System")
    {
        _db.JobAuditLogs.Add(new JobAuditLog
        {
            RepairJobId = repairJobId,
            Action = action,
            UserName = userName,
            CreatedAt = DateTime.Now
        });

        await _db.SaveChangesAsync();
    }
}