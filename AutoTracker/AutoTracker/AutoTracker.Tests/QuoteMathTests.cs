using AutoTracker.Services;

namespace AutoTracker.Tests;

public class QuoteMathTests
{
    [Fact]
    public void CalculateTotals_IncludesAllSectionsDiscountAndVat()
    {
        var result = QuoteMath.CalculateTotals(
            parts: 1000m,
            labour: 600m,
            paint: 300m,
            stripAndAssemble: 200m,
            panelBeating: 100m,
            polishing: 50m,
            consumables: 75m,
            sublet: 125m,
            discount: 50m,
            vatPercentage: 15m);

        Assert.Equal(2400m, result.Subtotal);
        Assert.Equal(360m, result.Vat);
        Assert.Equal(2760m, result.Total);
    }

    [Fact]
    public void CalculateTotals_DiscountAboveCharges_NeverProducesNegativeMoney()
    {
        var result = QuoteMath.CalculateTotals(
            parts: 100m,
            labour: 0m,
            paint: 0m,
            stripAndAssemble: 0m,
            panelBeating: 0m,
            polishing: 0m,
            consumables: 0m,
            sublet: 0m,
            discount: 150m,
            vatPercentage: 15m);

        Assert.Equal(0m, result.Subtotal);
        Assert.Equal(0m, result.Vat);
        Assert.Equal(0m, result.Total);
    }

    [Theory]
    [InlineData("Parts", 2, 125, 0, 0, 0, 250)]
    [InlineData("Labour", 0, 0, 3, 0, 100, 300)]
    [InlineData("Paint", 0, 0, 0, 2, 150, 300)]
    [InlineData("StripAndAssemble", 0, 0, 2.5, 0, 80, 200)]
    public void CalculateLineValue_UsesTheCorrectInputs(
        string section,
        decimal quantity,
        decimal unitPrice,
        decimal hours,
        decimal panels,
        decimal rate,
        decimal expected)
    {
        var result = QuoteMath.CalculateLineValue(section, quantity, unitPrice, hours, panels, rate);

        Assert.Equal(expected, result);
    }
}
