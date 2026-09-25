using AutoTracker.Data;
using AutoTracker.Filters;
using AutoTracker.Models;
using AutoTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoTracker.Controllers;

[RequireRole("Administrator", "Workshop Manager", "Receptionist")]
public class CalendarController : Controller
{
    private readonly AppDbContext _db;
    public CalendarController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? year, int? month)
    {
        var selectedMonth = new DateTime(year ?? DateTime.Today.Year, month ?? DateTime.Today.Month, 1);
        var start = selectedMonth;
        var end = selectedMonth.AddMonths(1);

        var jobs = await _db.RepairJobs.Include(j => j.Client).Include(j => j.Vehicle)
            .Where(j => j.EstimatedCompletionDate.HasValue && j.EstimatedCompletionDate.Value >= start && j.EstimatedCompletionDate.Value < end)
            .OrderBy(j => j.EstimatedCompletionDate).ToListAsync();

        var events = await _db.CalendarEvents.Include(e => e.RepairJob)
            .Where(e => e.StartTime >= start && e.StartTime < end)
            .OrderBy(e => e.StartTime).ToListAsync();

        return View(new CalendarViewModel { CurrentMonth = selectedMonth, Jobs = jobs, Events = events });
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent(string title, string description, DateTime startTime, DateTime? endTime, int? repairJobId)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["Error"] = "Calendar title is required.";
            return RedirectToAction(nameof(Index));
        }
        _db.CalendarEvents.Add(new CalendarEvent
        {
            Title = title.Trim(),
            Description = description ?? "",
            StartTime = startTime,
            EndTime = endTime,
            RepairJobId = repairJobId,
            EventType = repairJobId.HasValue ? "Repair" : "Manual",
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Calendar entry saved.";
        return RedirectToAction(nameof(Index), new { year = startTime.Year, month = startTime.Month });
    }
}
