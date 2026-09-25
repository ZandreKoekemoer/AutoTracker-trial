using AutoTracker.Models;
using AutoTracker.Services;

namespace AutoTracker.Tests;

public class JobFinanceServiceTests
{
    [Fact]
    public async Task RecalculateAsync_UsesInventoryPartsWhenNoCompletedPartsInvoiceExists()
    {
        using var database = new TestDatabase();
        var client = new Client { FullName = "Client" };
        var vehicle = new Vehicle { RegNumber = "FIN1", Client = client };
        var job = new RepairJob
        {
            Client = client,
            Vehicle = vehicle,
            Quote = new Quote { Total = 1000m },
            Parts =
            [
                new Part { PartNumber = "P1", Description = "Panel", Quantity = 2, ActualUnitCost = 100m }
            ]
        };
        database.Db.RepairJobs.Add(job);
        await database.Db.SaveChangesAsync();
        var service = new JobFinanceService(database.Db);

        await service.RecalculateAsync(job.Id);

        Assert.Equal(200m, job.ActualPartsCost);
        Assert.Equal(200m, job.ActualTotalCost);
        Assert.Equal(800m, job.Profit);
        Assert.Equal(80m, job.ProfitMargin);
    }
}
