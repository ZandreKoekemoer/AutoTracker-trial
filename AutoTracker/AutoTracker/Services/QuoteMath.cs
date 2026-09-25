namespace AutoTracker.Services;

public static class QuoteMath
{
    public sealed record Totals(decimal Subtotal, decimal Vat, decimal Total);

    public static Totals CalculateTotals(
        decimal parts,
        decimal labour,
        decimal paint,
        decimal stripAndAssemble,
        decimal panelBeating,
        decimal polishing,
        decimal consumables,
        decimal sublet,
        decimal discount,
        decimal vatPercentage)
    {
        var charges = parts + labour + paint + stripAndAssemble + panelBeating + polishing + consumables + sublet;
        var subtotal = Math.Max(0m, charges - Math.Max(0m, discount));
        var vat = subtotal * (Math.Max(0m, vatPercentage) / 100m);
        return new Totals(subtotal, vat, subtotal + vat);
    }

    public static decimal CalculateLineValue(
        string section,
        decimal quantity,
        decimal unitPrice,
        decimal hours,
        decimal panels,
        decimal rate)
    {
        return section switch
        {
            "Parts" => quantity * unitPrice,
            "Labour" => hours * rate,
            "Paint" => panels * rate,
            "StripAndAssemble" => hours * rate,
            _ => 0m
        };
    }
}
