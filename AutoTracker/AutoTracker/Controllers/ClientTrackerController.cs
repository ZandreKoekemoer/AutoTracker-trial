using AutoTracker.Services;
using AutoTracker.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace AutoTracker.Controllers;

public class ClientTrackerController : Controller
{
    private readonly ClientTrackingService _tracking;
    private readonly CompanySettingService _settings;
    private readonly IWebHostEnvironment _environment;

    public ClientTrackerController(ClientTrackingService tracking, CompanySettingService settings, IWebHostEnvironment environment)
    {
        _tracking = tracking;
        _settings = settings;
        _environment = environment;
    }

    public IActionResult Index() => View();

    public async Task<IActionResult> Job(string token)
    {
        SetPrivateResponseHeaders();
        var job = await _tracking.FindValidJobAsync(token);
        if (job == null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("AccessUnavailable");
        }

        var settings = _settings.GetSettings();
        var visibleUpdates = job.TimelineEntries
            .Where(x => x.IsClientVisible)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();
        var visiblePhotos = job.Photos
            .Where(x => x.IsClientVisible)
            .OrderByDescending(x => x.UploadedAt)
            .ToList();
        var lastUpdated = visibleUpdates.Select(x => x.CreatedAt)
            .Concat(visiblePhotos.Select(x => x.UploadedAt))
            .Append(job.CreatedAt)
            .Max();

        var model = new ClientTrackingViewModel
        {
            Token = token,
            RepairReference = $"AT-{job.Id:000000}",
            RegistrationNumber = job.Vehicle?.RegNumber ?? "",
            VehicleDescription = $"{job.Vehicle?.Make} {job.Vehicle?.ModelName}".Trim(),
            Status = job.Status,
            EstimatedCompletionDate = job.EstimatedCompletionDate,
            LastUpdated = lastUpdated,
            WorkshopName = string.IsNullOrWhiteSpace(settings.CompanyName) ? "AutoTracker" : settings.CompanyName,
            WorkshopPhone = settings.Phone,
            WorkshopEmail = settings.Email,
            Updates = visibleUpdates.Select(x => new ClientTrackingUpdateViewModel
            {
                Message = x.Message,
                CreatedAt = x.CreatedAt
            }).ToList(),
            Photos = visiblePhotos.Select(x => new ClientTrackingPhotoViewModel
            {
                Id = x.Id,
                Notes = x.Notes,
                UploadedAt = x.UploadedAt
            }).ToList()
        };
        return View(model);
    }

    public async Task<IActionResult> Media(string token, int id)
    {
        SetPrivateResponseHeaders();
        var job = await _tracking.FindValidJobAsync(token);
        var photo = job?.Photos.FirstOrDefault(x => x.Id == id && x.IsClientVisible);
        if (photo == null) return NotFound();

        var relative = photo.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_environment.WebRootPath, relative));
        var allowedRoot = Path.GetFullPath(Path.Combine(_environment.WebRootPath, "uploads")) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(fullPath))
            return NotFound();

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fullPath, out var contentType)) contentType = "application/octet-stream";
        return PhysicalFile(fullPath, contentType);
    }

    private void SetPrivateResponseHeaders()
    {
        Response.Headers.CacheControl = "no-store, private";
        Response.Headers.Pragma = "no-cache";
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        Response.Headers["Referrer-Policy"] = "no-referrer";
    }
}
