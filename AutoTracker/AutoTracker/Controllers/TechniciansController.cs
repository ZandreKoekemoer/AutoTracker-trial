using AutoTracker.Data;
using AutoTracker.Services;
using AutoTracker.ViewModels;
using AutoTracker.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoTracker.Controllers;

[RequireRole("Administrator", "Workshop Manager")]
public class TechniciansController : Controller
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public TechniciansController(AppDbContext db, AuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IActionResult> Index(int? userId)
    {
        var technicians = await _db.AppUsers
            .Where(u => u.IsActive && u.Role == "Technician")
            .OrderBy(u => u.FullName)
            .ToListAsync();

        var activeJobs = await _db.RepairJobs
            .Include(j => j.Client)
            .Include(j => j.Vehicle)
            .Where(j => j.Status != "Collected" && j.Status != "Archived")
            .OrderBy(j => j.AssignedTechnician)
            .ThenBy(j => j.CreatedAt)
            .ToListAsync();

        if (userId.HasValue)
        {
            var selected = technicians.FirstOrDefault(u => u.Id == userId.Value);
            activeJobs = selected == null
                ? []
                : activeJobs.Where(j => j.AssignedTechnicianUserId == selected.Id ||
                    (j.AssignedTechnicianUserId == null && j.AssignedTechnician == selected.FullName)).ToList();
        }

        var assigned = activeJobs
            .Where(j => j.AssignedTechnicianUserId.HasValue || !string.IsNullOrWhiteSpace(j.AssignedTechnician))
            .GroupBy(j => new { j.AssignedTechnicianUserId, j.AssignedTechnician })
            .Select(g => new TechnicianWorkloadItem
            {
                TechnicianName = g.Key.AssignedTechnician,
                Jobs = g.ToList()
            })
            .OrderBy(x => x.TechnicianName)
            .ToList();

        ViewBag.AppUsers = technicians;
        ViewBag.SelectedUserId = userId;

        return View(new TechnicianWorkloadViewModel
        {
            Technicians = assigned,
            UnassignedJobs = activeJobs.Where(j => j.AssignedTechnicianUserId == null && string.IsNullOrWhiteSpace(j.AssignedTechnician)).ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> AssignTechnician(int repairJobId, int? technicianUserId)
    {
        var job = await _db.RepairJobs.FindAsync(repairJobId);
        if (job == null) return NotFound();

        var technician = technicianUserId.HasValue
            ? await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == technicianUserId.Value && u.IsActive && u.Role == "Technician")
            : null;
        if (technicianUserId.HasValue && technician == null)
        {
            TempData["Error"] = "Choose a valid active technician.";
            return RedirectToAction(nameof(Index));
        }

        var oldUser = string.IsNullOrWhiteSpace(job.AssignedTechnician) ? "Unassigned" : job.AssignedTechnician;
        job.AssignedTechnicianUserId = technician?.Id;
        job.AssignedTechnician = technician?.FullName ?? "";

        await _db.SaveChangesAsync();
        await _audit.LogAsync(repairJobId, $"Assigned technician changed from {oldUser} to {(technician?.FullName ?? "Unassigned")}.");

        TempData["Success"] = "Assigned technician updated.";
        return RedirectToAction(nameof(Index), new { userId = technician?.Id });
    }
}
