using AutoTracker.Filters;
using Microsoft.AspNetCore.Mvc;

namespace AutoTracker.Controllers;

[RequireRole("Administrator")]
public class BackupController : Controller
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    public BackupController(IWebHostEnvironment env, IConfiguration config){_env=env;_config=config;}

    public IActionResult Index() => View();

    public IActionResult Download()
    {
        var dbPath = Path.Combine(_env.ContentRootPath, "autotracker.db");
        if (!System.IO.File.Exists(dbPath)) return NotFound("Database file not found.");
        var name = $"AutoTracker_Backup_{DateTime.Now:yyyyMMdd_HHmm}.db";
        return PhysicalFile(dbPath, "application/octet-stream", name);
    }
// Reference: Upload files in ASP.NET Core
// According to Microsoft (2026), IFormFile can receive uploaded files and CopyToAsync() can save them.
// I used this approach to restore an uploaded database backup.
    [HttpPost]
    public async Task<IActionResult> Restore(IFormFile backupFile)
    {
        if (backupFile == null || backupFile.Length == 0)
        {
            TempData["Error"] = "Choose a database backup file.";
            return RedirectToAction(nameof(Index));
        }
        var ext = Path.GetExtension(backupFile.FileName).ToLowerInvariant();
        if (ext != ".db" || backupFile.Length > 100 * 1024 * 1024)
        {
            TempData["Error"] = "Only .db SQLite backup files up to 100MB can be restored.";
            return RedirectToAction(nameof(Index));
        }
        var dbPath = Path.Combine(_env.ContentRootPath, "autotracker.db");
        var safetyCopy = Path.Combine(_env.ContentRootPath, $"autotracker_before_restore_{DateTime.Now:yyyyMMdd_HHmmss}.db");
        if (System.IO.File.Exists(dbPath)) System.IO.File.Copy(dbPath, safetyCopy, true);
        await using var stream = System.IO.File.Create(dbPath);
        await backupFile.CopyToAsync(stream);
        TempData["Success"] = "Backup restored. Restart the app so SQLite reconnects cleanly.";
        return RedirectToAction(nameof(Index));
    }
}
/*

Microsoft. 2026. Upload files in ASP.NET Core [Source code].
Available at:
https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0
[Accessed 17 August 2026].

*/