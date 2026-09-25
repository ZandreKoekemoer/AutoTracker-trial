using AutoTracker.Models;
using AutoTracker.Services;
using Microsoft.EntityFrameworkCore;

namespace AutoTracker.Tests;

public class ClientTrackingServiceTests
{
    [Fact]
    public async Task GenerateAsync_StoresOnlyAHashAndValidatesTheRawToken()
    {
        using var database = new TestDatabase();
        var client = new Client { FullName = "Test Client" };
        var vehicle = new Vehicle { RegNumber = "TEST123", Client = client };
        var job = new RepairJob { Client = client, Vehicle = vehicle };
        database.Db.RepairJobs.Add(job);
        await database.Db.SaveChangesAsync();
        var service = new ClientTrackingService(database.Db);

        var result = await service.GenerateAsync(job.Id, createdByUserId: 7, TimeSpan.FromDays(14));
        var stored = await database.Db.ClientTrackingAccesses.SingleAsync();

        Assert.NotEqual(result.RawToken, stored.TokenHash);
        Assert.DoesNotContain(result.RawToken, stored.TokenHash, StringComparison.Ordinal);
        Assert.Equal(64, stored.TokenHash.Length);
        Assert.NotNull(await service.FindValidJobAsync(result.RawToken));
    }

    [Fact]
    public async Task FindValidJobAsync_RejectsExpiredAndRevokedAccess()
    {
        using var database = new TestDatabase();
        var client = new Client { FullName = "Lifecycle Client" };
        var vehicle = new Vehicle { RegNumber = "LIFE123", Client = client };
        var job = new RepairJob { Client = client, Vehicle = vehicle };
        database.Db.RepairJobs.Add(job);
        await database.Db.SaveChangesAsync();
        var service = new ClientTrackingService(database.Db);

        var expired = await service.GenerateAsync(job.Id, 1, TimeSpan.FromMinutes(-1));
        Assert.Null(await service.FindValidJobAsync(expired.RawToken));

        var active = await service.GenerateAsync(job.Id, 1, TimeSpan.FromDays(1));
        Assert.NotNull(await service.FindValidJobAsync(active.RawToken));
        await service.RevokeAsync(job.Id);

        Assert.Null(await service.FindValidJobAsync(active.RawToken));
        Assert.Null(await service.GetCurrentAsync(job.Id));
    }

    [Fact]
    public async Task GenerateAsync_RevokesAnExistingActiveLink()
    {
        using var database = new TestDatabase();
        var client = new Client { FullName = "Rotation Client" };
        var vehicle = new Vehicle { RegNumber = "ROT123", Client = client };
        var job = new RepairJob { Client = client, Vehicle = vehicle };
        database.Db.RepairJobs.Add(job);
        await database.Db.SaveChangesAsync();
        var service = new ClientTrackingService(database.Db);

        var first = await service.GenerateAsync(job.Id, 1, TimeSpan.FromDays(7));
        var second = await service.GenerateAsync(job.Id, 1, TimeSpan.FromDays(7));

        Assert.NotEqual(first.RawToken, second.RawToken);
        Assert.Null(await service.FindValidJobAsync(first.RawToken));
        Assert.NotNull(await service.FindValidJobAsync(second.RawToken));
        Assert.Equal(2, await database.Db.ClientTrackingAccesses.CountAsync());
        Assert.Single(await database.Db.ClientTrackingAccesses.Where(x => x.RevokedAtUtc == null).ToListAsync());
    }
}
