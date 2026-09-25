using AutoTracker.Data;
using AutoTracker.Filters;
using AutoTracker.Models;
using AutoTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoTracker.Controllers;

[RequireRole("Administrator", "Workshop Manager")]
public class RfidTagsController : Controller
{
    private readonly AppDbContext _db;
    private readonly RfidService _rfidService;

    public RfidTagsController(AppDbContext db, RfidService rfidService)
    {
        _db = db;
        _rfidService = rfidService;
    }

    public async Task<IActionResult> Index()
    {
        var tags = await _db.RfidTags
            .Include(t => t.CurrentRepairJob)
                .ThenInclude(j => j!.Vehicle)
            .Include(t => t.CurrentRepairJob)
                .ThenInclude(j => j!.Client)
            .OrderBy(t => t.TagCode)
            .ToListAsync();

        return View(tags);
    }

    [HttpPost]
    public async Task<IActionResult> Create(string tagCode)
    {
        tagCode = RfidService.NormalizeTagCode(tagCode);
        if (string.IsNullOrWhiteSpace(tagCode))
        {
            TempData["Error"] = "RFID tag code is required.";
            return RedirectToAction(nameof(Index));
        }

        await _rfidService.GetOrCreateTag(tagCode);
        TempData["Success"] = "RFID tag created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Update(int id, string tagCode, bool isActive)
    {
        var tag = await _db.RfidTags.FindAsync(id);
        if (tag == null) return NotFound();

        tagCode = RfidService.NormalizeTagCode(tagCode);
        if (string.IsNullOrWhiteSpace(tagCode))
        {
            TempData["Error"] = "RFID tag code is required.";
            return RedirectToAction(nameof(Index));
        }

        if (await _db.RfidTags.AnyAsync(t => t.Id != id && t.TagCode == tagCode))
        {
            TempData["Error"] = "Another RFID tag already uses that code.";
            return RedirectToAction(nameof(Index));
        }

        if (!isActive && tag.CurrentRepairJobId != null)
        {
            TempData["Error"] = "Remove this tag from its current job before deactivating it.";
            return RedirectToAction(nameof(Index));
        }

        var oldCode = tag.TagCode;
        tag.TagCode = tagCode;
        tag.IsActive = isActive;

        if (oldCode != tagCode)
        {
            var jobs = await _db.RepairJobs.Where(j => j.RfidTagCode == oldCode).ToListAsync();
            foreach (var job in jobs) job.RfidTagCode = tagCode;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "RFID tag updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> RemoveAssignment(int id)
    {
        var tag = await _db.RfidTags.FindAsync(id);
        if (tag == null) return NotFound();

        await _rfidService.RemoveCurrentAssignment(tag.TagCode);
        TempData["Success"] = "RFID tag assignment removed.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var tag = await _db.RfidTags
            .Include(t => t.History)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (tag == null) return NotFound();

        if (tag.CurrentRepairJobId != null || tag.History.Any())
        {
            tag.IsActive = false;
            TempData["Success"] = "RFID tag has history, so it was deactivated instead of deleted.";
        }
        else
        {
            _db.RfidTags.Remove(tag);
            TempData["Success"] = "RFID tag deleted.";
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(string tagCode)
    {
        if (string.IsNullOrWhiteSpace(tagCode)) return NotFound();
        var tag = await _rfidService.GetTagWithHistory(tagCode);
        if (tag == null) return NotFound();
        return View(tag);
    }
}
