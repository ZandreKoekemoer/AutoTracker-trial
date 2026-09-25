using AutoTracker.Controllers;
using AutoTracker.Models;
using AutoTracker.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.FileProviders;

namespace AutoTracker.Tests;

public class RepairJobsWhatsappFlowTests
{
    [Fact]
    public async Task UpdateStatus_WithWhatsAppSelected_NavigatesDirectlyToPreparedMessage()
    {
        using var database = new TestDatabase();
        var client = new Client { FullName = "WhatsApp Client", Phone = "082 123 4567" };
        var vehicle = new Vehicle { Client = client, RegNumber = "WA123", Make = "Make", ModelName = "Model" };
        var job = new RepairJob { Client = client, Vehicle = vehicle, Status = "Booked In" };
        database.Db.RepairJobs.Add(job);
        await database.Db.SaveChangesAsync();
        var controller = CreateController(database);

        var result = await controller.UpdateStatus(job.Id, "Quote Pending", true, "Please review the update.");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.StartsWith("https://wa.me/27821234567?text=", redirect.Url);
        await database.Db.Entry(job).ReloadAsync();
        Assert.Equal("Quote Pending", job.Status);
    }

    private static RepairJobsController CreateController(TestDatabase database)
    {
        var environment = new TestEnvironment();
        var controller = new RepairJobsController(
            database.Db,
            new CompletionService(),
            environment,
            new RfidService(database.Db),
            new AuditService(database.Db),
            new ChecklistTemplateService(database.Db),
            new CompanySettingService(database.Db, environment),
            new JobFinanceService(database.Db),
            new WhatsappNotificationService(),
            new ClientTrackingService(database.Db));

        var http = new DefaultHttpContext();
        http.Features.Set<ISessionFeature>(new TestSessionFeature { Session = new TestSession() });
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        controller.TempData = new TempDataDictionary(http, new MemoryTempDataProvider());
        return controller;
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "AutoTracker.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class TestSessionFeature : ISessionFeature
    {
        public ISession Session { get; set; } = null!;
    }

    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _values = new();
        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public IEnumerable<string> Keys => _values.Keys;
        public void Clear() => _values.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _values.Remove(key);
        public void Set(string key, byte[] value) => _values[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _values.TryGetValue(key, out value!);
    }

    private sealed class MemoryTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
