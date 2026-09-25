using AutoTracker.Data;
using AutoTracker.Filters;
using AutoTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoTracker.Controllers;

[RequireRole("Administrator", "Workshop Manager", "Receptionist")]
public class SearchController : Controller
{
    private readonly AppDbContext _db;

    public SearchController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? q)
    {
        var model = new SearchResultViewModel
        {
            SearchTerm = q ?? ""
        };

        if (string.IsNullOrWhiteSpace(q))
            return View(model);

        q = q.Trim();

        model.Clients = await _db.Clients
            .Where(c =>
                c.FullName.Contains(q) ||
                c.Phone.Contains(q) ||
                c.Email.Contains(q))
            .OrderBy(c => c.FullName)
            .Take(20)
            .ToListAsync();

        model.Vehicles = await _db.Vehicles
            .Include(v => v.Client)
            .Where(v =>
                v.RegNumber.Contains(q) ||
                v.VinNumber.Contains(q) ||
                v.Make.Contains(q) ||
                v.ModelName.Contains(q) ||
                v.VehicleType.Contains(q))
            .OrderBy(v => v.RegNumber)
            .Take(20)
            .ToListAsync();

        model.RepairJobs = await _db.RepairJobs
            .Include(j => j.Client)
            .Include(j => j.Vehicle)
            .Where(j =>
                j.RfidTagCode.Contains(q) ||
                j.Status.Contains(q) ||
                j.AssignedTechnician.Contains(q) ||
                j.Priority.Contains(q) ||
                j.Client!.FullName.Contains(q) ||
                j.Vehicle!.RegNumber.Contains(q) ||
                j.Vehicle!.VinNumber.Contains(q))
            .OrderByDescending(j => j.CreatedAt)
            .Take(30)
            .ToListAsync();

        model.RfidTags = await _db.RfidTags
            .Include(t => t.CurrentRepairJob)
                .ThenInclude(j => j!.Vehicle)
            .Where(t => t.TagCode.Contains(q))
            .OrderBy(t => t.TagCode)
            .Take(20)
            .ToListAsync();

        return View(model);
    }
}