using AutoTracker.Data;
using AutoTracker.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutoTracker.Services;

public class JobAccessService
{
    private readonly AppDbContext _db;

    public JobAccessService(AppDbContext db) => _db = db;

    public IQueryable<RepairJob> ScopeJobs(IQueryable<RepairJob> query, ClaimsPrincipal user)
    {
        if (!user.IsInRole("Technician")) return query;
        if (!TryGetIdentity(user, out var userId, out var userName)) return query.Where(_ => false);

        return query.Where(j =>
            j.AssignedTechnicianUserId == userId ||
            (j.AssignedTechnicianUserId == null && j.AssignedTechnician == userName &&
             !_db.AppUsers.Any(u => u.IsActive && u.Id != userId && u.FullName == userName)));
    }

    public Task<bool> CanAccessAsync(ClaimsPrincipal user, int repairJobId)
    {
        if (!user.IsInRole("Technician")) return Task.FromResult(true);
        return ScopeJobs(_db.RepairJobs, user).AnyAsync(j => j.Id == repairJobId);
    }

    private static bool TryGetIdentity(ClaimsPrincipal user, out int userId, out string userName)
    {
        userName = user.FindFirstValue(ClaimTypes.Name) ?? "";
        return int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId) && userId > 0 && userName.Length > 0;
    }
}
