using AutoTracker.Data;
using AutoTracker.Filters;
using AutoTracker.Models;
using AutoTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoTracker.Controllers;

public class PartsController : Controller
{
    private static readonly string[] AllowedStatuses = ["Required", "Ordered", "Received", "Fitted", "Cancelled"];
    private readonly AppDbContext _db;
    private readonly PartCalculationService _partsCalc;
    private readonly AuditService _audit;

    public PartsController(AppDbContext db, PartCalculationService partsCalc, AuditService audit)
    {
        _db = db;
        _partsCalc = partsCalc;
        _audit = audit;
    }

    [RequireRole("Administrator", "Workshop Manager", "Receptionist")]
    [HttpPost]
    public async Task<IActionResult> AddPart(Part part)
    {
        if (!await _db.RepairJobs.AnyAsync(j => j.Id == part.RepairJobId)) return NotFound();
        if (!IsValid(part))
        {
            TempData["Error"] = "Enter a description, a quantity above zero, and non-negative prices.";
            return RedirectToInventory(part.RepairJobId);
        }

        Normalize(part);
        _db.Parts.Add(part);
        await _db.SaveChangesAsync();
        await _partsCalc.RecalculateJobCosts(part.RepairJobId);
        await _audit.LogAsync(part.RepairJobId, $"Part added: {part.Description}");
        TempData["Success"] = "Part added to inventory.";
        return RedirectToInventory(part.RepairJobId);
    }

    [RequireRole("Administrator", "Workshop Manager", "Receptionist")]
    [HttpPost]
    public async Task<IActionResult> UpdatePart(Part part)
    {
        var existing = await _db.Parts.FindAsync(part.Id);
        if (existing == null) return NotFound();
        part.RepairJobId = existing.RepairJobId;
        if (!IsValid(part))
        {
            TempData["Error"] = "Enter a description, a quantity above zero, and non-negative prices.";
            return RedirectToInventory(existing.RepairJobId);
        }

        existing.PartNumber = (part.PartNumber ?? "").Trim();
        existing.Description = part.Description.Trim();
        existing.Quantity = part.Quantity;
        existing.QuotedUnitPrice = part.QuotedUnitPrice;
        existing.ActualUnitCost = part.ActualUnitCost;
        existing.TotalQuotedPrice = part.Quantity * part.QuotedUnitPrice;
        existing.TotalActualCost = part.Quantity * part.ActualUnitCost;
        existing.Supplier = (part.Supplier ?? "").Trim();
        existing.Status = part.Status;
        existing.CriticalPart = part.CriticalPart;
        existing.InvoiceUploaded = part.InvoiceUploaded;
        existing.Notes = (part.Notes ?? "").Trim();

        await _db.SaveChangesAsync();
        await _partsCalc.RecalculateJobCosts(existing.RepairJobId);
        await _audit.LogAsync(existing.RepairJobId, $"Part updated: {existing.Description}");
        TempData["Success"] = "Part updated.";
        return RedirectToInventory(existing.RepairJobId);
    }

    [RequireRole("Administrator", "Workshop Manager", "Receptionist", "Technician")]
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, string status, int repairJobId, string? returnTo)
    {
        var part = await _db.Parts.FindAsync(id);
        if (part == null || part.RepairJobId != repairJobId) return NotFound();
        if (!AllowedStatuses.Contains(status)) return BadRequest();

        part.Status = status;
        await _db.SaveChangesAsync();
        await _partsCalc.RecalculateJobCosts(part.RepairJobId);
        await _audit.LogAsync(part.RepairJobId, $"Part '{part.Description}' marked {status}");
        return RedirectToInventory(part.RepairJobId);
    }

    [RequireRole("Administrator", "Workshop Manager")]
    [HttpPost]
    public async Task<IActionResult> DeletePart(int id)
    {
        var part = await _db.Parts.FindAsync(id);
        if (part == null) return NotFound();
        var repairJobId = part.RepairJobId;
        _db.Parts.Remove(part);
        await _db.SaveChangesAsync();
        await _partsCalc.RecalculateJobCosts(repairJobId);
        await _audit.LogAsync(repairJobId, $"Part deleted: {part.Description}");
        TempData["Success"] = "Part removed from inventory.";
        return RedirectToInventory(repairJobId);
    }

    private static bool IsValid(Part part) =>
        !string.IsNullOrWhiteSpace(part.Description) &&
        part.Quantity > 0 &&
        part.QuotedUnitPrice >= 0 &&
        part.ActualUnitCost >= 0 &&
        AllowedStatuses.Contains(part.Status);

    private static void Normalize(Part part)
    {
        part.PartNumber = (part.PartNumber ?? "").Trim();
        part.Description = part.Description.Trim();
        part.Supplier = (part.Supplier ?? "").Trim();
        part.Notes = (part.Notes ?? "").Trim();
        part.TotalQuotedPrice = part.Quantity * part.QuotedUnitPrice;
        part.TotalActualCost = part.Quantity * part.ActualUnitCost;
        part.VehicleDamageItemId = null;
    }

    private RedirectToActionResult RedirectToInventory(int repairJobId) =>
        RedirectToAction("Inventory", "RepairJobs", new { id = repairJobId });
}
