using AutoTracker.Data;
using AutoTracker.Models;
using AutoTracker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace AutoTracker.Tests;

public class CompanySettingServiceTests
{
    [Fact]
    public async Task NextQuoteNumber_IsUniqueAcrossConcurrentContexts()
    {
        var path = Path.Combine(Path.GetTempPath(), $"autotracker-number-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={path};Default Timeout=30")
            .Options;
        try
        {
            await using (var setup = new AppDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync();
                setup.CompanySettings.Add(new CompanySetting { CompanyName = "Test", QuotePrefix = "EST", InvoicePrefix = "INV" });
                await setup.SaveChangesAsync();
            }

            var numbers = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
            {
                using var context = new AppDbContext(options);
                return new CompanySettingService(context, new TestEnvironment()).NextQuoteNumber();
            })));

            Assert.Equal(8, numbers.Distinct().Count());
            Assert.Equal(Enumerable.Range(1, 8).Select(i => $"EST-{i:0000}").Order(), numbers.Order());
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            File.Delete(path);
            File.Delete(path + "-wal");
            File.Delete(path + "-shm");
        }
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
}
