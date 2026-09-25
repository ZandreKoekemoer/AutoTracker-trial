using AutoTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoTracker.Services;

public class QuoteCalculationService
{
    private readonly AppDbContext _db;
    private readonly CompanySettingService _settings;

    public QuoteCalculationService(AppDbContext db, CompanySettingService settings)
    {
        _db = db;
        _settings = settings;
    }

    public async Task RecalculateQuote(int repairJobId)
    {
        var quote = await _db.Quotes
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.RepairJobId == repairJobId);
        if (quote == null) return;

        quote.Parts = quote.LineItems.Where(x => x.Section == "Parts").Sum(x => x.Value);
        quote.Labour = quote.LineItems.Where(x => x.Section == "Labour").Sum(x => x.Value);
        quote.Paint = quote.LineItems.Where(x => x.Section == "Paint").Sum(x => x.Value);
        quote.StripAndAssemble = quote.LineItems.Where(x => x.Section == "StripAndAssemble").Sum(x => x.Value);

        var totals = QuoteMath.CalculateTotals(
            quote.Parts,
            quote.Labour,
            quote.Paint,
            quote.StripAndAssemble,
            quote.PanelBeating,
            quote.Polishing,
            quote.Consumables,
            quote.Sublet,
            quote.Discount,
            _settings.GetSettings().VatPercentage);
        quote.Subtotal = totals.Subtotal;
        quote.Vat = totals.Vat;
        quote.Total = totals.Total;
        quote.IsCompleted = quote.Total > 0;
        quote.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
    }
}
