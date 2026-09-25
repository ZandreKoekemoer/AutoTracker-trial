using AutoTracker.Data;
using AutoTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoTracker.Services;

namespace AutoTracker.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly CompletionService _completion;
    private readonly JobAccessService _jobAccess;

    public HomeController(AppDbContext db, CompletionService completion, JobAccessService jobAccess)
    {
        _db = db;
        _completion = completion;
        _jobAccess = jobAccess;
    }

    public async Task<IActionResult> Index()
    {
        var role = HttpContext.Session.GetString("UserRole") ?? "";
        var canSeeFinancials = role is "Administrator" or "Workshop Manager";
        ViewBag.CanSeeFinancials = canSeeFinancials;

        var scopedJobs = _jobAccess.ScopeJobs(_db.RepairJobs.AsNoTracking(), User);
        var jobs = await scopedJobs
            .Include(j => j.Client)
            .Include(j => j.Vehicle)
            .Include(j => j.Documents)
            .Include(j => j.Quote)
            .Include(j => j.Checklist)
            .Where(j => j.Status != "Archived" && j.Status != "Collected")
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();

        var missingJobs = jobs
            .Where(j => _completion.GetMissingRequirements(j).Any())
            .ToList();
        var scopedJobIds = _jobAccess.ScopeJobs(_db.RepairJobs.AsNoTracking(), User).Select(j => j.Id);
        var now = DateTime.Now;

        var model = new DashboardViewModel
        {
            TotalActiveJobs = jobs.Count,
            WaitingApproval = jobs.Count(j => j.Status == "Waiting Approval"),
            ReadyForCollection = jobs.Count(j => j.Status == "Ready"),
            OverdueJobs = jobs.Count(j =>
                j.EstimatedCompletionDate.HasValue &&
                j.EstimatedCompletionDate.Value.Date < DateTime.Today &&
                j.Status != "Ready" &&
                j.Status != "Collected"),
            MissingDocuments = missingJobs.Count,
            CollectedThisMonth = await _jobAccess.ScopeJobs(_db.RepairJobs.AsNoTracking(), User).CountAsync(j =>
                j.Status == "Collected" &&
                j.CompletedAt.HasValue &&
                j.CompletedAt.Value.Month == now.Month &&
                j.CompletedAt.Value.Year == now.Year),
            MonthlyProfit = canSeeFinancials
                ? (await _jobAccess.ScopeJobs(_db.RepairJobs.AsNoTracking(), User)
                    .Where(j => j.CompletedAt.HasValue && j.CompletedAt.Value.Month == now.Month && j.CompletedAt.Value.Year == now.Year)
                    .Select(j => j.Profit)
                    .ToListAsync()).Sum()
                : 0,
            RecentJobs = jobs.Take(8).ToList(),
            JobsWithMissingRequirements = missingJobs.Take(8).ToList(),
            RecentActivity = await _db.JobAuditLogs
                .Where(a => scopedJobIds.Contains(a.RepairJobId))
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .ToListAsync()
        };

        return View(model);
    }
}
