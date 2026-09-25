using AutoTracker.Data;
using AutoTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoTracker.Services;

public class RfidService
{
    private readonly AppDbContext _db;

    public RfidService(AppDbContext db) => _db = db;

    public sealed record TagOperationResult(bool Success, string Message, int? RepairJobId = null);

    public async Task<RfidTag> GetOrCreateTag(string tagCode)
    {
        tagCode = NormalizeTagCode(tagCode);
        var tag = await _db.RfidTags
            .Include(t => t.History)
            .FirstOrDefaultAsync(t => t.TagCode == tagCode);

        if (tag != null) return tag;

        tag = new RfidTag
        {
            TagCode = tagCode,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        _db.RfidTags.Add(tag);
        await _db.SaveChangesAsync();
        return tag;
    }

    public async Task<TagOperationResult> AssignTagToRepair(string tagCode, int repairJobId)
    {
        tagCode = NormalizeTagCode(tagCode);
        if (string.IsNullOrWhiteSpace(tagCode))
            return new(false, "RFID tag code is required.");

        var tag = await _db.RfidTags.FirstOrDefaultAsync(t => t.TagCode == tagCode);
        if (tag == null) return new(false, "Tag could not be found.");
        if (!tag.IsActive) return new(false, "This tag is disabled.");

        var job = await _db.RepairJobs.FindAsync(repairJobId);
        if (job == null) return new(false, "Repair job could not be found.");
        if (job.Status is "Collected" or "Archived")
            return new(false, "A tag cannot be assigned to a completed or archived repair job.");

        if (tag.CurrentRepairJobId.HasValue && tag.CurrentRepairJobId.Value != repairJobId)
            return new(false, "This tag is already assigned.", tag.CurrentRepairJobId);

        if (tag.CurrentRepairJobId == repairJobId && job.RfidTagCode == tagCode)
            return new(true, "RFID tag is already assigned to this repair job.", repairJobId);

        if (!string.IsNullOrWhiteSpace(job.RfidTagCode) && !string.Equals(job.RfidTagCode, tagCode, StringComparison.OrdinalIgnoreCase))
            await RemoveCurrentAssignment(job.RfidTagCode);

        tag.CurrentRepairJobId = repairJobId;
        job.RfidTagCode = tagCode;

        var existingOpenHistory = await _db.RfidTagHistories
            .FirstOrDefaultAsync(h => h.RfidTagId == tag.Id && h.RepairJobId == repairJobId && h.RemovedAt == null);
        if (existingOpenHistory == null)
        {
            _db.RfidTagHistories.Add(new RfidTagHistory
            {
                RfidTagId = tag.Id,
                RepairJobId = repairJobId,
                AssignedAt = DateTime.Now,
                Notes = "Tag assigned to repair job."
            });
        }

        await _db.SaveChangesAsync();
        return new(true, "RFID tag assigned successfully.", repairJobId);
    }

    public async Task<TagOperationResult> FindAssignedRepairAsync(string tagCode)
    {
        tagCode = NormalizeTagCode(tagCode);
        if (string.IsNullOrWhiteSpace(tagCode)) return new(false, "Scan or enter a tag code.");

        var tag = await _db.RfidTags
            .Include(t => t.CurrentRepairJob)
            .FirstOrDefaultAsync(t => t.TagCode == tagCode);
        if (tag == null) return new(false, "Tag could not be found.");
        if (!tag.IsActive) return new(false, "This tag is disabled.");
        if (tag.CurrentRepairJobId == null || tag.CurrentRepairJob == null)
            return new(false, "This tag is not assigned to a repair job.");
        if (tag.CurrentRepairJob.Status is "Collected" or "Archived")
            return new(false, "This tag is assigned to a completed or archived repair job.");

        return new(true, "Repair job found.", tag.CurrentRepairJobId);
    }

    public async Task RemoveCurrentAssignment(string tagCode)
    {
        tagCode = NormalizeTagCode(tagCode);
        var tag = await _db.RfidTags.FirstOrDefaultAsync(t => t.TagCode == tagCode);
        if (tag == null) return;

        var openHistory = await _db.RfidTagHistories
            .Where(h => h.RfidTagId == tag.Id && h.RemovedAt == null)
            .ToListAsync();
        foreach (var history in openHistory) history.RemovedAt = DateTime.Now;

        if (tag.CurrentRepairJobId != null)
        {
            var job = await _db.RepairJobs.FindAsync(tag.CurrentRepairJobId.Value);
            if (job != null && string.Equals(job.RfidTagCode, tagCode, StringComparison.OrdinalIgnoreCase))
                job.RfidTagCode = "";
        }

        tag.CurrentRepairJobId = null;
        await _db.SaveChangesAsync();
    }

    public async Task<RfidTag?> GetTagWithHistory(string tagCode)
    {
        tagCode = NormalizeTagCode(tagCode);
        return await _db.RfidTags
            .Include(t => t.CurrentRepairJob)!.ThenInclude(j => j!.Vehicle)
            .Include(t => t.CurrentRepairJob)!.ThenInclude(j => j!.Client)
            .Include(t => t.History).ThenInclude(h => h.RepairJob)!.ThenInclude(j => j!.Vehicle)
            .Include(t => t.History).ThenInclude(h => h.RepairJob)!.ThenInclude(j => j!.Client)
            .FirstOrDefaultAsync(t => t.TagCode == tagCode);
    }

    public static string NormalizeTagCode(string? tagCode) => (tagCode ?? "").Trim().ToUpperInvariant();
}
