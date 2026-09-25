using AutoTracker.Controllers;
using AutoTracker.Models;
using AutoTracker.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AutoTracker.Tests;

public class WorkshopBoardControllerTests
{
    [Fact]
    public async Task MoveStatus_RejectsCollectedWithoutCollectionEvidence()
    {
        using var database = new TestDatabase();
        var job = await AddReadyJob(database);
        var controller = CreateController(database);

        await controller.MoveStatus(job.Id, "Collected");

        await database.Db.Entry(job).ReloadAsync();
        Assert.Equal("Ready", job.Status);
        Assert.Null(job.CompletedAt);
        Assert.Null(job.CollectedAt);
    }

    [Fact]
    public async Task MoveStatusAjax_RejectsCollectedWithoutCollectionEvidence()
    {
        using var database = new TestDatabase();
        var job = await AddReadyJob(database);
        var controller = CreateController(database);

        var result = await controller.MoveStatusAjax(new WorkshopBoardController.MoveStatusRequest
        {
            JobId = job.Id,
            NewStatus = "Collected"
        });

        var json = Assert.IsType<JsonResult>(result);
        var success = Assert.IsType<bool>(json.Value!.GetType().GetProperty("success")!.GetValue(json.Value));
        Assert.False(success);
        await database.Db.Entry(job).ReloadAsync();
        Assert.Equal("Ready", job.Status);
        Assert.Null(job.CompletedAt);
        Assert.Null(job.CollectedAt);
    }

    [Fact]
    public async Task MoveStatus_DoesNotReopenCollectedJob()
    {
        using var database = new TestDatabase();
        var job = await AddReadyJob(database);
        job.Status = "Collected";
        job.CompletedAt = DateTime.UtcNow;
        job.CollectedAt = DateTime.UtcNow;
        await database.Db.SaveChangesAsync();
        var controller = CreateController(database);

        await controller.MoveStatus(job.Id, "Ready");

        await database.Db.Entry(job).ReloadAsync();
        Assert.Equal("Collected", job.Status);
        Assert.NotNull(job.CompletedAt);
        Assert.NotNull(job.CollectedAt);
    }

    [Fact]
    public async Task MoveStatusAjax_DoesNotReopenCollectedJob()
    {
        using var database = new TestDatabase();
        var job = await AddReadyJob(database);
        job.Status = "Collected";
        job.CompletedAt = DateTime.UtcNow;
        job.CollectedAt = DateTime.UtcNow;
        await database.Db.SaveChangesAsync();
        var controller = CreateController(database);

        var result = await controller.MoveStatusAjax(new WorkshopBoardController.MoveStatusRequest
        {
            JobId = job.Id,
            NewStatus = "Ready"
        });

        var json = Assert.IsType<JsonResult>(result);
        var success = Assert.IsType<bool>(json.Value!.GetType().GetProperty("success")!.GetValue(json.Value));
        Assert.False(success);
        await database.Db.Entry(job).ReloadAsync();
        Assert.Equal("Collected", job.Status);
        Assert.NotNull(job.CompletedAt);
        Assert.NotNull(job.CollectedAt);
    }

    private static WorkshopBoardController CreateController(TestDatabase database)
    {
        var controller = new WorkshopBoardController(database.Db, new CompletionService(), new AuditService(database.Db));
        var http = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        controller.TempData = new TempDataDictionary(http, new MemoryTempDataProvider());
        return controller;
    }

    private static async Task<RepairJob> AddReadyJob(TestDatabase database)
    {
        var client = new Client { FullName = "Board Client" };
        var vehicle = new Vehicle { RegNumber = "BOARD1", Make = "Make", ModelName = "Model", Client = client };
        var job = new RepairJob { Client = client, Vehicle = vehicle, Status = "Ready" };
        database.Db.RepairJobs.Add(job);
        await database.Db.SaveChangesAsync();
        return job;
    }

    private sealed class MemoryTempDataProvider : ITempDataProvider
    {
        private Dictionary<string, object> _values = new();
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>(_values);
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) => _values = new Dictionary<string, object>(values);
    }
}
