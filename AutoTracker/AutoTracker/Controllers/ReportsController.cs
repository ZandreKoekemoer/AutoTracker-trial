using AutoTracker.Data;
using AutoTracker.Filters;
using AutoTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoTracker.Controllers;

[RequireRole("Administrator", "Workshop Manager")]
public class ReportsController : Controller
{
    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.Now;

        var jobs = await _db.RepairJobs
            .Include(j => j.Quote)
            .ToListAsync();

        var completedJobs = jobs
            .Where(j => j.CompletedAt.HasValue)
            .ToList();

        var completedThisMonth = jobs
            .Where(j =>
                j.CompletedAt.HasValue &&
                j.CompletedAt.Value.Month == now.Month &&
                j.CompletedAt.Value.Year == now.Year)
            .ToList();

        var model = new ReportsViewModel
        {
            TotalJobs = jobs.Count,

            ActiveJobs = jobs.Count(j =>
                j.Status != "Collected" &&
                j.Status != "Archived"),

            CompletedJobs = completedJobs.Count,

            OverdueJobs = jobs.Count(j =>
                j.EstimatedCompletionDate.HasValue &&
                j.EstimatedCompletionDate.Value.Date < DateTime.Today &&
                j.Status != "Ready" &&
                j.Status != "Collected" &&
                j.Status != "Archived"),

            MonthlyTurnover = completedThisMonth
                .Where(j => j.Quote != null)
                .Sum(j => j.Quote!.Total),

            MonthlyCosts = completedThisMonth.Sum(j => j.ActualTotalCost),
            MonthlyProfit = completedThisMonth.Sum(j => j.Profit),

            AverageRepairDays = completedJobs.Any()
                ? completedJobs.Average(j => (j.CompletedAt!.Value - j.CreatedAt).TotalDays)
                : 0,

            JobsByStatus = jobs
                .GroupBy(j => j.Status)
                .Select(g => new StatusReportItem
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList()
        };

        return View(model);
    }
}