using AutoTracker.Data;
using AutoTracker.Services;
using AutoTracker.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoTracker.Controllers;

[RequireRole("Administrator", "Workshop Manager")]
public class WorkshopBoardController : Controller
{
    private readonly AppDbContext _db;
    private readonly CompletionService _completion;
    private readonly AuditService _audit;

    private static readonly List<string> Statuses = new()
    {
        "Booked In",
        "Quote Pending",
        "Quote Sent",
        "Waiting Approval",
        "Approved",
        "Parts Ordered",
        "Parts Received",
        "Panel Beating",
        "Prep",
        "Paint",
        "Reassembly",
        "Quality Check",
        "Ready"
    };

    public WorkshopBoardController(AppDbContext db, CompletionService completion, AuditService audit)
    {
        _db = db;
        _completion = completion;
        _audit = audit;
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
            .OrderBy(j => j.CreatedAt)
            .ToListAsync();

        ViewBag.Statuses = Statuses;
        return View(jobs);
    }

    [HttpPost]
    public async Task<IActionResult> MoveStatus(int id, string newStatus)
    {
        var job = await _db.RepairJobs
            .Include(j => j.Documents)
            .Include(j => j.Quote)
            .Include(j => j.Checklist)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null) return NotFound();

        if (job.Status == "Collected")
        {
            TempData["Error"] = "Collected jobs cannot be moved on the workshop board.";
            return RedirectToAction(nameof(Index));
        }

        if (!Statuses.Contains(newStatus))
        {
            TempData["Error"] = "Invalid status.";
            return RedirectToAction(nameof(Index));
        }

        var missing = _completion.GetMissingRequirements(job);

        if (newStatus == "Approved" && missing.Any())
        {
            TempData["Error"] = "Cannot approve job. Missing: " + string.Join(", ", missing);
            return RedirectToAction(nameof(Index));
        }

        if (newStatus == "Ready")
        {
            var readyMissing = _completion.GetMissingReadyRequirements(job);

            if (job.Checklist == null || !job.Checklist.IsCompleted)
                readyMissing.Insert(0, "Checklist");

            if (readyMissing.Any())
            {
                TempData["Error"] = "Cannot mark Ready. Missing: " + string.Join(", ", readyMissing);
                return RedirectToAction(nameof(Index));
            }
        }

        var oldStatus = job.Status;
        job.Status = newStatus;

        await _db.SaveChangesAsync();
        await _audit.LogAsync(job.Id, $"Workflow moved from {oldStatus} to {newStatus}.");

        TempData["Success"] = "Job moved successfully.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    public async Task<IActionResult> MoveStatusAjax([FromBody] MoveStatusRequest request)
    {
        var job = await _db.RepairJobs
            .Include(j => j.Documents)
            .Include(j => j.Quote)
            .Include(j => j.Checklist)
            .FirstOrDefaultAsync(j => j.Id == request.JobId);

        if (job == null)
            return Json(new { success = false, message = "Job not found." });

        if (job.Status == "Collected")
            return Json(new { success = false, message = "Collected jobs cannot be moved on the workshop board." });

        if (!Statuses.Contains(request.NewStatus))
            return Json(new { success = false, message = "Invalid status." });

        var missing = _completion.GetMissingRequirements(job);

        if (request.NewStatus == "Approved" && missing.Any())
            return Json(new { success = false, message = "Cannot approve. Missing: " + string.Join(", ", missing) });

        if (request.NewStatus == "Ready")
        {
            var readyMissing = _completion.GetMissingReadyRequirements(job);

            if (job.Checklist == null || !job.Checklist.IsCompleted)
                readyMissing.Insert(0, "Checklist");

            if (readyMissing.Any())
                return Json(new { success = false, message = "Cannot mark Ready. Missing: " + string.Join(", ", readyMissing) });
        }

        var oldStatus = job.Status;
        job.Status = request.NewStatus;

        await _db.SaveChangesAsync();
        await _audit.LogAsync(job.Id, $"Dragged from {oldStatus} to {request.NewStatus}.");

        return Json(new { success = true });
    }

    public class MoveStatusRequest
    {
        public int JobId { get; set; }
        public string NewStatus { get; set; } = "";
    }
}