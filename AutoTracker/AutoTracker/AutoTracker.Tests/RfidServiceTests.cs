using AutoTracker.Models;
using AutoTracker.Services;

namespace AutoTracker.Tests;

public class RfidServiceTests
{
    [Fact]
    public async Task AssignTagToRepair_RejectsUnknownTag()
    {
        using var database = new TestDatabase();
        var job = await AddJob(database, "CAR1");
        var service = new RfidService(database.Db);

        var result = await service.AssignTagToRepair("UNKNOWN", job.Id);

        Assert.False(result.Success);
        Assert.Equal("Tag could not be found.", result.Message);
        Assert.Equal(string.Empty, job.RfidTagCode);
    }

    [Fact]
    public async Task AssignTagToRepair_PreventsDuplicateActiveAssignment()
    {
        using var database = new TestDatabase();
        var firstJob = await AddJob(database, "CAR1");
        var secondJob = await AddJob(database, "CAR2");
        database.Db.RfidTags.Add(new RfidTag { TagCode = "TAG-1", IsActive = true, CurrentRepairJobId = firstJob.Id });
        firstJob.RfidTagCode = "TAG-1";
        await database.Db.SaveChangesAsync();
        var service = new RfidService(database.Db);

        var result = await service.AssignTagToRepair("TAG-1", secondJob.Id);

        Assert.False(result.Success);
        Assert.Equal("This tag is already assigned.", result.Message);
        Assert.Equal("TAG-1", firstJob.RfidTagCode);
        Assert.Equal(string.Empty, secondJob.RfidTagCode);
    }

    [Fact]
    public async Task AssignTagToRepair_RejectsInactiveTag()
    {
        using var database = new TestDatabase();
        var job = await AddJob(database, "CAR3");
        database.Db.RfidTags.Add(new RfidTag { TagCode = "TAG-OFF", IsActive = false });
        await database.Db.SaveChangesAsync();
        var service = new RfidService(database.Db);

        var result = await service.AssignTagToRepair(" tag-off ", job.Id);

        Assert.False(result.Success);
        Assert.Equal("This tag is disabled.", result.Message);
        Assert.Equal(string.Empty, job.RfidTagCode);
    }

    private static async Task<RepairJob> AddJob(TestDatabase database, string registration)
    {
        var client = new Client { FullName = "Test Client " + registration };
        var vehicle = new Vehicle { RegNumber = registration, Client = client };
        var job = new RepairJob { Client = client, Vehicle = vehicle, Status = "Booked In" };
        database.Db.RepairJobs.Add(job);
        await database.Db.SaveChangesAsync();
        return job;
    }
}
