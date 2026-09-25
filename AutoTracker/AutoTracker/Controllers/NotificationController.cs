using AutoTracker.Data;
using AutoTracker.ViewModels;
using AutoTracker.Services;
using AutoTracker.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoTracker.Controllers;

[RequireRole("Administrator", "Workshop Manager")]
public class NotificationsController : Controller
{
    private readonly AppDbContext _db;
    private readonly CompletionService _completion;

    public NotificationsController(AppDbContext db, CompletionService completion)
    {
        _db = db;
        _completion = completion;
    }

    public async Task<IActionResult> Index()
    {
        var jobs = await _db.RepairJobs
            .Include(j => j.Client)
            .Include(j => j.Vehicle)
            .Include(j => j.Documents)
            .Include(j => j.Quote)
            .Include(j => j.Checklist)
            .Where(j => j.Status != "Archived" && j.Status != "Collected")
            .ToListAsync();

        var model = new NotificationViewModel();

        foreach (var job in jobs)
        {
            var missing = _completion.GetMissingRequirements(job);

            foreach (var item in missing)
            {
                model.Items.Add(new NotificationItem
                {
                    Type = "Missing Requirement",
                    Message = $"{job.Vehicle?.RegNumber} - Missing {item}",
                    Severity = "Danger",
                    RepairJobId = job.Id,
                    CreatedAt = DateTime.Now
                });
            }

            if (job.Status == "Waiting Approval")
            {
                model.Items.Add(new NotificationItem
                {
                    Type = "Approval",
                    Message = $"{job.Vehicle?.RegNumber} is waiting for approval.",
                    Severity = "Warning",
                    RepairJobId = job.Id
                });
            }

            if (job.Status == "Parts Ordered")
            {
                model.Items.Add(new NotificationItem
                {
                    Type = "Parts",
                    Message = $"{job.Vehicle?.RegNumber} is waiting for parts.",
                    Severity = "Info",
                    RepairJobId = job.Id
                });
            }

            if (job.EstimatedCompletionDate.HasValue &&
                job.EstimatedCompletionDate.Value.Date < DateTime.Today &&
                job.Status != "Ready")
            {
                model.Items.Add(new NotificationItem
                {
                    Type = "Overdue",
                    Message = $"{job.Vehicle?.RegNumber} is overdue.",
                    Severity = "Danger",
                    RepairJobId = job.Id
                });
            }
        }

        model.Items = model.Items
            .OrderByDescending(x => x.Severity == "Danger")
            .ThenByDescending(x => x.Severity == "Warning")
            .ThenBy(x => x.Type)
            .ToList();

        return View(model);
    }
}