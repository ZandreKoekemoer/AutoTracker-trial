using AutoTracker.Data;
using AutoTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoTracker.Services;

public class JobFinanceService
{
    private readonly AppDbContext _db;
    public JobFinanceService(AppDbContext db) => _db = db;

    public async Task RecalculateAsync(int repairJobId)
    {
        var job = await _db.RepairJobs
            .Include(j => j.Quote)
            .Include(j => j.Documents)
            .Include(j => j.Parts)
            .FirstOrDefaultAsync(j => j.Id == repairJobId);

        if (job == null) return;

        var completedCostDocs = job.Documents
            .Where(d => d.IsCompleted && d.CostAmount > 0 && DocumentTypes.IsCostDocument(d.DocumentType))
            .ToList();

        foreach (var part in job.Parts)
        {
            part.TotalQuotedPrice = part.Quantity * part.QuotedUnitPrice;
            part.TotalActualCost = part.Quantity * part.ActualUnitCost;
        }

        var completedPartsInvoices = completedCostDocs
            .Where(d => DocumentTypes.Normalize(d.DocumentType) == DocumentTypes.PartsInvoice)
            .Sum(d => d.CostAmount);

        job.ActualPartsCost = completedPartsInvoices > 0
            ? completedPartsInvoices
            : job.Parts.Sum(p => p.TotalActualCost);

        job.ActualLabourCost = completedCostDocs
            .Where(d => DocumentTypes.Normalize(d.DocumentType) == DocumentTypes.LabourInvoice)
            .Sum(d => d.CostAmount);

        job.ActualPaintCost = completedCostDocs
            .Where(d => DocumentTypes.Normalize(d.DocumentType) == DocumentTypes.PaintInvoice)
            .Sum(d => d.CostAmount);

        job.ActualConsumablesCost = completedCostDocs
            .Where(d => DocumentTypes.Normalize(d.DocumentType) == DocumentTypes.ConsumablesInvoice)
            .Sum(d => d.CostAmount);

        job.ActualSubletCost = completedCostDocs
            .Where(d => DocumentTypes.Normalize(d.DocumentType) == DocumentTypes.SupplierInvoice)
            .Sum(d => d.CostAmount);

        job.ActualTotalCost = job.ActualPartsCost + job.ActualLabourCost + job.ActualPaintCost + job.ActualConsumablesCost + job.ActualSubletCost;

        var income = job.Quote?.Total ?? 0m;
        job.Profit = income - job.ActualTotalCost;
        job.ProfitMargin = income > 0 ? Math.Round((job.Profit / income) * 100m, 2) : 0m;

        await _db.SaveChangesAsync();
    }

    public async Task<decimal> GetMonthlyProfitAsync(int year, int month)
    {
        var profits = await _db.RepairJobs
            .Where(j => j.CompletedAt.HasValue && j.CompletedAt.Value.Year == year && j.CompletedAt.Value.Month == month)
            .Select(j => j.Profit)
            .ToListAsync();

        return profits.Sum();
    }
}
