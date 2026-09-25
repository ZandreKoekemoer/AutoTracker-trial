using AutoTracker.Data;
using AutoTracker.Filters;
using AutoTracker.Models;
using AutoTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AutoTracker.Controllers;

public class RepairJobsController : Controller
{
    // Helper: returns true if the current user is a Technician
    private bool IsTechnician() =>
        (HttpContext.Session.GetString("UserRole") ?? "") == "Technician";

    private readonly AppDbContext _db;
    private readonly CompletionService _completion;
    private readonly IWebHostEnvironment _env;
    private readonly RfidService _rfidService;
    private readonly AuditService _audit;
    private readonly ChecklistTemplateService _checklistTemplate;
    private readonly CompanySettingService _companySettings;
    private readonly JobFinanceService _finance;
    private readonly WhatsappNotificationService _whatsapp;
    private readonly ClientTrackingService _tracking;

    private static readonly List<string> AllStatuses = new()
    {
        "Booked In",
        "Quote Pending",
        "Quote Sent",
        "Waiting Approval",
        "Approved",
        "Parts Ordered",
        "Parts Received",
        "Panel Beating",
        "Prep",
        "Paint",
        "Reassembly",
        "Quality Check",
        "Ready",
        "Collected",
        "Archived"
    };

    public RepairJobsController(
        AppDbContext db,
        CompletionService completion,
        IWebHostEnvironment env,
        RfidService rfidService,
        AuditService audit,
        ChecklistTemplateService checklistTemplate,
        CompanySettingService companySettings,
        JobFinanceService finance,
        WhatsappNotificationService whatsapp,
        ClientTrackingService tracking)
    {
        _db = db;
        _completion = completion;
        _env = env;
        _rfidService = rfidService;
        _audit = audit;
        _checklistTemplate = checklistTemplate;
        _companySettings = companySettings;
        _finance = finance;
        _whatsapp = whatsapp;
        _tracking = tracking;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _db.RepairJobs
            .Include(j => j.Client)
            .Include(j => j.Vehicle)
            .AsQueryable();

        if (IsTechnician())
        {
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? "";
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedId) ? parsedId : -1;
            query = query.Where(j => j.AssignedTechnicianUserId == userId ||
                (j.AssignedTechnicianUserId == null && j.AssignedTechnician == userName &&
                 !_db.AppUsers.Any(u => u.IsActive && u.Id != userId && u.FullName == userName)));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(j =>
                j.RfidTagCode.Contains(search) ||
                j.Status.Contains(search) ||
                j.Vehicle!.RegNumber.Contains(search) ||
                j.Client!.FullName.Contains(search));
        }

        return View(await query.OrderByDescending(j => j.CreatedAt).ToListAsync());
    }

    [RequireRole("Administrator", "Workshop Manager", "Receptionist")]
    public async Task<IActionResult> Create()
    {
        await LoadLists();
        return View(new RepairJob());
    }

    [HttpPost]
    [RequireRole("Administrator", "Workshop Manager", "Receptionist")]
    public async Task<IActionResult> Create(RepairJob job)
    {
        ModelState.Clear();

        if (job.VehicleId <= 0)
        {
            var clientName = Request.Form["ClientName"].ToString();
            var regNumber = Request.Form["RegNumber"].ToString();

            if (string.IsNullOrWhiteSpace(clientName) || string.IsNullOrWhiteSpace(regNumber))
            {
                TempData["Error"] = "Select an existing vehicle or enter a client name and registration number.";
                await LoadLists();
                return View(job);
            }

            var client = new Client
            {
                FullName = clientName.Trim(),
                Phone = Request.Form["ClientPhone"].ToString(),
                Email = Request.Form["ClientEmail"].ToString(),
                Address = Request.Form["ClientAddress"].ToString()
            };
            _db.Clients.Add(client);
            await _db.SaveChangesAsync();

            var vehicle = new Vehicle
            {
                ClientId = client.Id,
                RegNumber = regNumber.Trim().ToUpper(),
                VehicleType = Request.Form["VehicleType"].ToString(),
                Make = Request.Form["Make"].ToString(),
                ModelName = Request.Form["ModelName"].ToString(),
                VinNumber = Request.Form["VinNumber"].ToString()
            };
            _db.Vehicles.Add(vehicle);
            await _db.SaveChangesAsync();

            job.ClientId = client.Id;
            job.VehicleId = vehicle.Id;
        }
        else
        {
            var vehicle = await _db.Vehicles.FindAsync(job.VehicleId);
            if (vehicle == null)
            {
                TempData["Error"] = "Selected vehicle was not found.";
                await LoadLists();
                return View(job);
            }
            job.ClientId = vehicle.ClientId;
        }

        var requestedTechnicianId = job.AssignedTechnicianUserId;
        AppUser? requestedTechnician = null;
        if (requestedTechnicianId.HasValue)
        {
            requestedTechnician = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == requestedTechnicianId.Value && u.IsActive && u.Role == "Technician");
            if (requestedTechnician == null)
            {
                TempData["Error"] = "Choose a valid active technician.";
                await LoadLists();
                return View(job);
            }
        }

        job.Status = "Booked In";
        job.AssignedTechnician = requestedTechnician?.FullName ?? "";
        job.AssignedTechnicianUserId = requestedTechnician?.Id;
        job.TrackingToken = "";
        job.CompletedAt = null;
        job.CollectedAt = null;
        job.CollectionCustomerName = "";
        job.CollectionSignaturePath = "";
        job.ActualPartsCost = 0;
        job.ActualLabourCost = 0;
        job.ActualPaintCost = 0;
        job.ActualConsumablesCost = 0;
        job.ActualSubletCost = 0;
        job.ActualTotalCost = 0;
        job.Profit = 0;
        job.ProfitMargin = 0;
        if (string.IsNullOrWhiteSpace(job.RfidTagCode)) job.RfidTagCode = "";
        if (string.IsNullOrWhiteSpace(job.Priority)) job.Priority = "Normal";
        if (string.IsNullOrWhiteSpace(job.Notes)) job.Notes = "";
        if (string.IsNullOrWhiteSpace(job.InternalNotes)) job.InternalNotes = "";
        if (string.IsNullOrWhiteSpace(job.CollectionCustomerName)) job.CollectionCustomerName = "";
        if (string.IsNullOrWhiteSpace(job.CollectionSignaturePath)) job.CollectionSignaturePath = "";
        job.TrackingToken = "";
        _db.RepairJobs.Add(job);
        await _db.SaveChangesAsync();

        _db.Quotes.Add(new Quote { RepairJobId = job.Id, QuoteNumber = _companySettings.NextQuoteNumber(), QuoteDate = DateTime.Now, Terms = _companySettings.GetSettings().QuoteFooterText });
        _db.Checklists.Add(new Checklist { RepairJobId = job.Id });
        await _db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(job.RfidTagCode))
        {
            var scannedTag = job.RfidTagCode;
            job.RfidTagCode = "";
            var tagResult = await _rfidService.AssignTagToRepair(scannedTag, job.Id);
            if (!tagResult.Success) TempData["Error"] = tagResult.Message;
        }

        await _audit.LogAsync(job.Id, "Repair job created.");
        TempData["Success"] = "Repair job created.";
        return RedirectToAction(nameof(Details), new { id = job.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var job = await _db.RepairJobs
            .Include(j => j.Client)
            .Include(j => j.Vehicle)
            .Include(j => j.Documents)
            .Include(j => j.Quote)
                .ThenInclude(q => q!.LineItems)
            .Include(j => j.Checklist)
            .Include(j => j.AuditLogs)
            .Include(j => j.Photos)
            .Include(j => j.TimelineEntries)
            .Include(j => j.CustomerSignatures)
            .Include(j => j.Parts)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job is null) return NotFound();
        if (IsTechnician() && !await TechnicianCanAccessJobAsync(job.Id))
            return Forbid();

        ViewBag.Missing = _completion.GetMissingRequirements(job);
        ViewBag.Statuses = AllStatuses;
        ViewBag.AppUsers = await _db.AppUsers.Where(u => u.IsActive && u.Role == "Technician").OrderBy(u => u.FullName).ToListAsync();
        ViewBag.AvailableRfidTags = await _db.RfidTags.Where(t => t.IsActive && (t.CurrentRepairJobId == null || t.CurrentRepairJobId == job.Id)).OrderBy(t => t.TagCode).ToListAsync();
        await _checklistTemplate.EnsureChecklistItemsAsync(job.Id);

        ViewBag.ChecklistItems = await _db.ChecklistItems
            .Where(x => x.RepairJobId == job.Id)
            .OrderBy(x => x.Section)
            .ThenBy(x => x.ItemName)
            .ToListAsync();
        ViewBag.Parts = await _db.Parts
            .Where(p => p.RepairJobId == job.Id)
            .OrderBy(p => p.Description)
            .ToListAsync();
        ViewBag.TrackingAccess = await _tracking.GetCurrentAsync(job.Id);
        return View(job);
    }
    // Reference: Signature Pad – handling data URI encoded images on the server
    // According to Szimek (2025), signature data can be decoded from Base64 and stored as an image file.
    // I used this approach to convert the submitted signature into a PNG file.
    
    [HttpPost]
    [RequireRole("Administrator", "Workshop Manager", "Receptionist")]
    public async Task<IActionResult> SaveSignature(int repairJobId, string customerName, string signatureData)
    {
        if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(signatureData))
        {
            TempData["Error"] = "Customer name and signature are required.";
            return RedirectToAction(nameof(Details), new { id = repairJobId });
        }

        if (!TryDecodePngSignature(signatureData, out var bytes))
        {
            TempData["Error"] = "The signature data is invalid or too large.";
            return RedirectToAction(nameof(Details), new { id = repairJobId });
        }
        customerName = customerName.Trim();
        if (customerName.Length > 200) customerName = customerName[..200];

        var uploads = Path.Combine(_env.WebRootPath, "uploads", repairJobId.ToString(), "signatures");
        Directory.CreateDirectory(uploads);

        var fileName = $"signature_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}.png";
        var filePath = Path.Combine(uploads, fileName);

        await System.IO.File.WriteAllBytesAsync(filePath, bytes);

        _db.CustomerSignatures.Add(new CustomerSignature
        {
            RepairJobId = repairJobId,
            CustomerName = customerName,
            SignaturePath = $"/uploads/{repairJobId}/signatures/{fileName}",
            SignatureType = "Authorization",
            SignedAt = DateTime.Now
        });

        _db.JobDocuments.Add(new JobDocument
        {
            RepairJobId = repairJobId,
            DocumentType = "Authorization",
            FileName = fileName,
            FilePath = $"/uploads/{repairJobId}/signatures/{fileName}",
            IsCompleted = true,
            UploadedAt = DateTime.Now
        });

        await _db.SaveChangesAsync();
        await _audit.LogAsync(repairJobId, "Customer authorization signature saved.");

        return RedirectToAction(nameof(Details), new { id = repairJobId });
    }


    [HttpPost]
    public async Task<IActionResult> AddTimelineEntry(int repairJobId, string entryType, string message, bool isClientVisible = false)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "Timeline message is required.";
            return RedirectToAction(nameof(Details), new { id = repairJobId });
        }

        isClientVisible = !IsTechnician() && isClientVisible;
        _db.JobTimelineEntries.Add(new JobTimelineEntry
        {
            RepairJobId = repairJobId,
            EntryType = entryType,
            Message = message.Trim(),
            CreatedBy = HttpContext.Session.GetString("UserName") ?? "System",
            IsClientVisible = isClientVisible,
            CreatedAt = DateTime.Now
        });

        await _db.SaveChangesAsync();
        await _audit.LogAsync(repairJobId, $"Timeline entry added: {entryType}");

        return RedirectToAction(nameof(Details), new { id = repairJobId });
    }
    [HttpPost]
    public async Task<IActionResult> UpdateChecklistItem(int id, int repairJobId, string condition, string notes)
    {
        var allowedConditions = new HashSet<string>(["Not Checked", "Good", "Damaged", "Missing", "N/A"], StringComparer.Ordinal);
        var belongsToJob = await _db.ChecklistItems.AnyAsync(x => x.Id == id && x.RepairJobId == repairJobId);
        if (!belongsToJob) return NotFound();
        if (!allowedConditions.Contains(condition))
        {
            TempData["Error"] = "Choose a valid checklist condition.";
            return RedirectToAction(nameof(Checklist), new { id = repairJobId });
        }
        notes = (notes ?? "").Trim();
        if (notes.Length > 1000) notes = notes[..1000];

        await _checklistTemplate.UpdateChecklistItemAsync(id, condition, notes);

        var complete = await _checklistTemplate.IsCompleteAsync(repairJobId);

        var checklist = await _db.Checklists.FirstOrDefaultAsync(x => x.RepairJobId == repairJobId);
        if (checklist != null)
        {
            checklist.IsCompleted = complete;
            checklist.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
        }

        await _audit.LogAsync(repairJobId, $"Checklist item updated: {condition}");

        return RedirectToAction(nameof(Details), new { id = repairJobId });
    }
    // Reference: Upload files in ASP.NET Core
    // According to Microsoft (2026), uploaded files can be received using IFormFile and saved using CopyToAsync().
    // I used this approach to process and save vehicle photos. 
    [HttpPost]
    public async Task<IActionResult> UploadPhotos(int repairJobId, string category, string notes, bool isClientVisible, List<IFormFile> files)
    {
        if (files == null || !files.Any(f => f.Length > 0))
            return RedirectToAction(nameof(Photos), new { id = repairJobId });

        category = DocumentTypes.Normalize(category);
        category = category == DocumentTypes.FinalPhotos ? DocumentTypes.FinalPhotos : DocumentTypes.BeforePhotos;
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".heic" };
        var saved = 0;

        isClientVisible = !IsTechnician() && isClientVisible;
        notes = (notes ?? "").Trim();
        if (notes.Length > 1000) notes = notes[..1000];

        foreach (var file in files.Where(f => f.Length > 0))
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext) || file.Length > 10 * 1024 * 1024) continue;
            var uploads = Path.Combine(_env.WebRootPath, "uploads", repairJobId.ToString(), "photos", category);
            Directory.CreateDirectory(uploads);
            var safeName = $"{DateTime.Now:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(uploads, safeName);
            await using var stream = System.IO.File.Create(path);
            await file.CopyToAsync(stream);
            _db.JobPhotos.Add(new JobPhoto
            {
                RepairJobId = repairJobId,
                Category = category,
                FileName = file.FileName,
                FilePath = $"/uploads/{repairJobId}/photos/{category}/{safeName}",
                Notes = notes ?? "",
                IsClientVisible = isClientVisible,
                UploadedAt = DateTime.Now
            });
            saved++;
        }

        if (saved > 0 && !_db.JobDocuments.AsEnumerable().Any(d => d.RepairJobId == repairJobId && DocumentTypes.Normalize(d.DocumentType) == category && d.IsCompleted))
        {
            _db.JobDocuments.Add(new JobDocument
            {
                RepairJobId = repairJobId,
                DocumentType = category,
                FileName = $"{category} gallery",
                FilePath = $"/RepairJobs/Photos/{repairJobId}",
                IsCompleted = true,
                UploadedAt = DateTime.Now
            });
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(repairJobId, $"{saved} {category} uploaded.");
        TempData["Success"] = $"{saved} photo(s) uploaded.";
        return RedirectToAction(nameof(Photos), new { id = repairJobId });
    }
    [HttpPost]
    [RequireRole("Administrator", "Workshop Manager", "Receptionist")]
    public async Task<IActionResult> SetPhotoVisibility(int id, int repairJobId, bool isClientVisible)
    {
        var photo = await _db.JobPhotos.FirstOrDefaultAsync(x => x.Id == id && x.RepairJobId == repairJobId);
        if (photo == null) return NotFound();
        photo.IsClientVisible = isClientVisible;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(repairJobId, isClientVisible ? "Photo approved for client tracking." : "Photo hidden from client tracking.");
        TempData["Success"] = isClientVisible ? "Photo is now visible to the client." : "Photo is now hidden from the client.";
        return RedirectToAction(nameof(Photos), new { id = repairJobId });
    }

    // Reference: Upload files in ASP.NET Core
    [HttpPost]
    [RequireRole("Administrator", "Workshop Manager", "Receptionist")]
    public async Task<IActionResult> UploadDocument(int repairJobId, string documentType, decimal costAmount, string costCategory, bool manualCostOverride, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return RedirectToAction(nameof(Details), new { id = repairJobId });

        documentType = DocumentTypes.Normalize(documentType);
        var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".webp", ".heic", ".doc", ".docx", ".xls", ".xlsx" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext) || file.Length > 10 * 1024 * 1024)
        {
            TempData["Error"] = "Only safe document/photo files up to 10MB are allowed.";
            return RedirectToAction(nameof(Documents), new { id = repairJobId });
        }

        var uploads = Path.Combine(_env.WebRootPath, "uploads", repairJobId.ToString(), "documents");
        Directory.CreateDirectory(uploads);

        var safeName = $"{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(uploads, safeName);

        await using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream);

        _db.JobDocuments.Add(new JobDocument
        {
            RepairJobId = repairJobId,
            DocumentType = documentType,
            FileName = file.FileName,
            FilePath = $"/uploads/{repairJobId}/documents/{safeName}",
            IsCompleted = true,
            CostAmount = costAmount,
            CostCategory = DocumentTypes.Normalize(string.IsNullOrWhiteSpace(costCategory) ? documentType : costCategory),
            ManualCostOverride = manualCostOverride
        });

        await _db.SaveChangesAsync();
        await _finance.RecalculateAsync(repairJobId);
        await _audit.LogAsync(repairJobId, $"Document uploaded: {documentType}");

        return RedirectToAction(nameof(Documents), new { id = repairJobId });
    }
    // Reference: Quotation generator – structured quotation and calculation layout
    // According to Callmephil (2025), quotation items can be organised and totals calculated before producing the final quotation.
    // I used this approach to process the quote sections and calculate the subtotal, VAT and total.
    [HttpPost]
    [RequireRole("Administrator", "Workshop Manager", "Receptionist")]
    public async Task<IActionResult> UpdateQuote(int repairJobId)
    {
        var job = await _db.RepairJobs
            .Include(j => j.Quote)
                .ThenInclude(q => q!.LineItems)
            .FirstOrDefaultAsync(j => j.Id == repairJobId);

        if (job?.Quote == null) return NotFound();

        var quote = job.Quote;
        var settings = _companySettings.GetSettings();

        if (string.IsNullOrWhiteSpace(quote.QuoteNumber))
            quote.QuoteNumber = _companySettings.NextQuoteNumber();

        quote.Notes = Request.Form["Notes"].ToString() ?? "";
        quote.Terms = Request.Form["Terms"].ToString() ?? settings.QuoteFooterText;
        quote.Consumables = ReadDecimal("Consumables", settings.DefaultConsumables);
        quote.Sublet = ReadDecimal("Sublet", 0);
        quote.Discount = ReadDecimal("Discount", 0);
        if (quote.Consumables < 0 || quote.Sublet < 0 || quote.Discount < 0)
        {
            TempData["Error"] = "Estimate totals cannot be negative.";
            return RedirectToAction(nameof(Quote), new { id = repairJobId });
        }

        var oldItems = await _db.QuoteLineItems.Where(x => x.QuoteId == quote.Id).ToListAsync();
        _db.QuoteLineItems.RemoveRange(oldItems);

        var sections = Request.Form["Section"].ToList();
        var methods = Request.Form["Method"].ToList();
        var descriptions = Request.Form["Description"].ToList();
        var codes = Request.Form["Code"].ToList();
        var quantities = Request.Form["Quantity"].ToList();
        var prices = Request.Form["UnitPrice"].ToList();
        var hours = Request.Form["Hours"].ToList();
        var panels = Request.Form["Panels"].ToList();
        var values = Request.Form["Value"].ToList();

        decimal partsTotal = 0, labourTotal = 0, paintTotal = 0, stripTotal = 0;
        var quotedPartItems = new List<QuoteLineItem>();

        var allowedSections = new HashSet<string>(["Parts", "Labour", "Paint", "StripAndAssemble"], StringComparer.Ordinal);
        for (var i = 0; i < sections.Count; i++)
        {
            var section = sections[i] ?? "";
            var description = GetAt(descriptions, i).Trim();
            if (string.IsNullOrWhiteSpace(description)) continue;
            if (!allowedSections.Contains(section))
            {
                TempData["Error"] = "The estimate contains an invalid section.";
                return RedirectToAction(nameof(Quote), new { id = repairJobId });
            }

            var qty = ReadListDecimal(quantities, i);
            var unitPrice = ReadListDecimal(prices, i);
            var hrs = ReadListDecimal(hours, i);
            var pnl = ReadListDecimal(panels, i);
            var value = ReadListDecimal(values, i);
            if (section == "Parts" && (qty <= 0 || qty != decimal.Truncate(qty)))
            {
                TempData["Error"] = "Part quantities must be positive whole numbers.";
                return RedirectToAction(nameof(Quote), new { id = repairJobId });
            }
            if (qty < 0 || unitPrice < 0 || hrs < 0 || pnl < 0 || value < 0)
            {
                TempData["Error"] = "Estimate values cannot be negative.";
                return RedirectToAction(nameof(Quote), new { id = repairJobId });
            }

            if (value <= 0)
            {
                var rate = section switch
                {
                    "Labour" => settings.LabourRatePerHour,
                    "Paint" => settings.PaintRatePerPanel,
                    "StripAndAssemble" => settings.StripAssembleRatePerHour,
                    _ => 0m
                };
                value = QuoteMath.CalculateLineValue(section, qty, unitPrice, hrs, pnl, rate);
            }

            var item = new QuoteLineItem
            {
                RepairJobId = repairJobId,
                QuoteId = quote.Id,
                Section = section,
                Method = GetAt(methods, i),
                Description = description,
                Code = GetAt(codes, i),
                Quantity = qty,
                UnitPrice = unitPrice,
                Hours = hrs,
                Panels = pnl,
                Value = value,
                SortOrder = i
            };

            _db.QuoteLineItems.Add(item);

            if (section == "Parts") { partsTotal += value; quotedPartItems.Add(item); }
            else if (section == "Labour") labourTotal += value;
            else if (section == "Paint") paintTotal += value;
            else if (section == "StripAndAssemble") stripTotal += value;
        }

        quote.Parts = partsTotal;
        quote.Labour = labourTotal;
        quote.Paint = paintTotal;
        quote.StripAndAssemble = stripTotal;
        var grossCharges = quote.Parts + quote.Labour + quote.Paint + quote.StripAndAssemble +
            quote.PanelBeating + quote.Polishing + quote.Consumables + quote.Sublet;
        if (quote.Discount > grossCharges)
        {
            TempData["Error"] = "Discount cannot exceed the estimate charges.";
            return RedirectToAction(nameof(Quote), new { id = repairJobId });
        }
        var totals = QuoteMath.CalculateTotals(
            quote.Parts,
            quote.Labour,
            quote.Paint,
            quote.StripAndAssemble,
            quote.PanelBeating,
            quote.Polishing,
            quote.Consumables,
            quote.Sublet,
            quote.Discount,
            settings.VatPercentage);
        quote.Subtotal = totals.Subtotal;
        quote.Vat = totals.Vat;
        quote.Total = totals.Total;
        quote.IsCompleted = quote.Total > 0;
        quote.UpdatedAt = DateTime.Now;

        await SyncQuotedPartsToInventoryAsync(repairJobId, quotedPartItems);

        await _db.SaveChangesAsync();
        await _finance.RecalculateAsync(repairJobId);
        await _audit.LogAsync(repairJobId, "Estimate updated.");
        TempData["Success"] = "Estimate saved.";
        return RedirectToAction(nameof(Quote), new { id = repairJobId });
    }

    [HttpPost]
    [RequireRole("Administrator", "Workshop Manager", "Receptionist")]
    public async Task<IActionResult> GenerateInvoiceNumber(int repairJobId)
    {
        var quote = await _db.Quotes.FirstOrDefaultAsync(x => x.RepairJobId == repairJobId);
        if (quote == null) return NotFound();
        if (string.IsNullOrWhiteSpace(quote.InvoiceNumber))
        {
            quote.InvoiceNumber = _companySettings.NextInvoiceNumber();
            quote.InvoiceDate = DateTime.Now;
            await _db.SaveChangesAsync();
            await _audit.LogAsync(repairJobId, $"Invoice number generated: {quote.InvoiceNumber}");
        }
        return RedirectToAction(nameof(Quote), new { id = repairJobId });
    }

    [HttpPost]
    public async Task<IActionResult> SaveChecklistItems(int repairJobId)
    {
        var items = await _db.ChecklistItems
            .Where(x => x.RepairJobId == repairJobId)
            .ToListAsync();

        foreach (var item in items)
        {
            var allowedConditions = new HashSet<string>(["Not Checked", "Good", "Damaged", "Missing", "N/A"], StringComparer.Ordinal);
            var submittedCondition = Request.Form[$"condition_{item.Id}"].ToString();
            item.Condition = allowedConditions.Contains(submittedCondition) ? submittedCondition : "Not Checked";
            var submittedNotes = (Request.Form[$"notes_{item.Id}"].ToString() ?? "").Trim();
            item.Notes = submittedNotes.Length > 1000 ? submittedNotes[..1000] : submittedNotes;
            item.UpdatedAt = DateTime.Now;
        }

        
        var complete = items.Any() &&
                       items.All(x => !string.IsNullOrWhiteSpace(x.Condition) &&
                                      x.Condition != "Not Checked");

        var checklist = await _db.Checklists.FirstOrDefaultAsync(x => x.RepairJobId == repairJobId);
        if (checklist == null)
        {
            checklist = new Checklist { RepairJobId = repairJobId };
            _db.Checklists.Add(checklist);
        }

        checklist.IsCompleted = complete;
        checklist.UpdatedAt = DateTime.Now;

        var checklistDoc = await _db.JobDocuments
            .FirstOrDefaultAsync(x => x.RepairJobId == repairJobId &&
                                      DocumentTypes.Normalize(x.DocumentType) == DocumentTypes.Checklist);

        if (complete)
        {
            if (checklistDoc == null)
            {
                _db.JobDocuments.Add(new JobDocument
                {
                    RepairJobId = repairJobId,
                    DocumentType = DocumentTypes.Checklist,
                    FileName = "Checklist",
                    FilePath = $"/RepairJobs/ChecklistPdf/{repairJobId}",
                    IsCompleted = true,
                    UploadedAt = DateTime.Now
                });
            }
            else
            {
                checklistDoc.DocumentType = DocumentTypes.Checklist;
                checklistDoc.FileName = "Checklist";
                checklistDoc.FilePath = $"/RepairJobs/ChecklistPdf/{repairJobId}";
                checklistDoc.IsCompleted = true;
                checklistDoc.UploadedAt = DateTime.Now;
            }

            TempData["Success"] = "Checklist saved and marked complete.";
        }
        else
        {
            if (checklistDoc != null)
            {
                checklistDoc.IsCompleted = false;
            }

            TempData["Error"] = "Checklist saved, but not everything has been checked yet.";
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(repairJobId, "Full checklist saved.");
        return RedirectToAction(nameof(Checklist), new { id = repairJobId });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateChecklist(Checklist checklist)
    {
        var existing = await _db.Checklists.FirstOrDefaultAsync(x => x.Id == checklist.Id && x.RepairJobId == checklist.RepairJobId);
        if (existing == null) return NotFound();

        existing.VehicleReceived = checklist.VehicleReceived;
        existing.DamagePhotosTaken = checklist.DamagePhotosTaken;
        existing.AuthorizationConfirmed = checklist.AuthorizationConfirmed;
        existing.PartsChecked = checklist.PartsChecked;
        existing.RepairCompleted = checklist.RepairCompleted;
        existing.QualityChecked = checklist.QualityChecked;
        existing.FinalPhotosTaken = checklist.FinalPhotosTaken;
        existing.ClientNotified = checklist.ClientNotified;
        existing.IsCompleted =
            existing.VehicleReceived &&
            existing.DamagePhotosTaken &&
            existing.AuthorizationConfirmed &&
            existing.PartsChecked &&
            existing.RepairCompleted &&
            existing.QualityChecked &&
            existing.FinalPhotosTaken &&
            existing.ClientNotified;

        existing.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(existing.RepairJobId, "Checklist updated.");

        return RedirectToAction(nameof(Details), new { id = existing.RepairJobId });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, string status, bool notifyClient, string clientNote)
    {
        var job = await _db.RepairJobs
            .Include(j => j.Client)
            .Include(j => j.Vehicle)
            .Include(j => j.Documents)
            .Include(j => j.Quote)
                .ThenInclude(q => q!.LineItems)
            .Include(j => j.Checklist)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job is null) return NotFound();

        if (!AllStatuses.Contains(status))
        {
            TempData["Error"] = "Invalid job status.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (status == "Collected")
        {
            TempData["Error"] = "Use the collection-signature action to mark a vehicle Collected.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (job.Status == "Collected" && status != "Archived")
        {
            TempData["Error"] = "A collected job can only be archived.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (status == "Archived" && job.Status != "Collected")
        {
            TempData["Error"] = "Only a collected repair can be archived.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var missing = _completion.GetMissingRequirements(job);

        if (status == "Approved" && missing.Any())
        {
            TempData["Error"] = "This repair cannot be approved yet. Missing requirements: " + string.Join(", ", missing);
            return RedirectToAction(nameof(Details), new { id });
        }

        if (status == "Ready")
        {
            var readyMissing = _completion.GetMissingReadyRequirements(job);
            if (job.Checklist == null || !job.Checklist.IsCompleted)
                readyMissing.Insert(0, "Checklist");

            if (readyMissing.Any())
            {
                TempData["Error"] = "This repair cannot be marked Ready yet. Missing: " + string.Join(", ", readyMissing);
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        clientNote = (clientNote ?? "").Trim();
        if (clientNote.Length > 500) clientNote = clientNote[..500];
        job.Status = status;
        _db.JobTimelineEntries.Add(new JobTimelineEntry
        {
            RepairJobId = job.Id,
            EntryType = "Status Update",
            Message = $"Repair status changed to {status}.",
            CreatedBy = HttpContext.Session.GetString("UserName") ?? "System",
            IsClientVisible = true,
            CreatedAt = DateTime.Now
        });

        if (status == "Collected")
            job.CompletedAt = DateTime.Now;

        await _db.SaveChangesAsync();
        await _audit.LogAsync(id, $"Status changed to {status}." + (string.IsNullOrWhiteSpace(clientNote) ? "" : $" Client note: {clientNote}"));

        if (notifyClient && job.Client != null && !string.IsNullOrWhiteSpace(job.Client.Phone))
        {
            var message = _whatsapp.BuildStatusMessage(job, clientNote, null);
            var whatsappUrl = _whatsapp.BuildWhatsAppUrl(job.Client.Phone, message);
            if (!string.IsNullOrEmpty(whatsappUrl))
            {
                TempData["Success"] = "Repair status updated. WhatsApp opened with a prepared message; review it and press Send.";
                return Redirect(whatsappUrl);
            }
            else
            {
                TempData["Success"] = "Repair status updated.";
                TempData["Info"] = "The client's phone number could not be used for WhatsApp.";
            }
        }
        else
        {
            TempData["Success"] = "Repair status updated.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Scan(string tag)
    {
        var result = await _rfidService.FindAssignedRepairAsync(tag);
        if (result.Success && result.RepairJobId.HasValue)
        {
            if (IsTechnician() && !await TechnicianCanAccessJobAsync(result.RepairJobId.Value)) return Forbid();
            return RedirectToAction(nameof(Details), new { id = result.RepairJobId.Value });
        }

        TempData["Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [RequireRole("Administrator", "Workshop Manager", "Receptionist")]
    public async Task<IActionResult> GenerateClientTracking(int repairJobId, int validDays = 14)
    {
        var job = await _db.RepairJobs.Include(j => j.Client).FirstOrDefaultAsync(j => j.Id == repairJobId);
        if (job == null) return NotFound();

        var days = Math.Clamp(validDays, 1, 90);
        var actorUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var generated = await _tracking.GenerateAsync(repairJobId, actorUserId, TimeSpan.FromDays(days));
        var trackingUrl = Url.Action(nameof(ClientTrackerController.Job), "ClientTracker", new { token = generated.RawToken }, Request.Scheme) ?? "";

        TempData["TrackingLink"] = trackingUrl;
        TempData["Success"] = $"A new client tracking link was created for {days} days. Copy it now; AutoTracker stores only its secure hash.";
        var message = _whatsapp.BuildStatusMessage(job, "You can follow the repair progress here:", trackingUrl);
        var whatsappUrl = _whatsapp.BuildWhatsAppUrl(job.Client?.Phone ?? "", message);
        if (!string.IsNullOrEmpty(whatsappUrl))
            TempData["TrackingWhatsAppUrl"] = whatsappUrl;

        await _audit.LogAsync(repairJobId, $"Client tracking link generated for {days} days.");
        return RedirectToAction(nameof(Details), new { id = repairJobId });
    }

    [HttpPost]
    [RequireRole("Administrator", "Workshop Manager", "Receptionist")]
    public async Task<IActionResult> RevokeClientTracking(int repairJobId)
    {
        if (await _db.RepairJobs.FindAsync(repairJobId) == null) return NotFound();
        await _tracking.RevokeAsync(repairJobId);
        await _audit.LogAsync(repairJobId, "Client tracking access revoked.");
        TempData["Success"] = "Client tracking access has been revoked.";
        return RedirectToAction(nameof(Details), new { id = repairJobId });
    }

    [HttpPost]
    [RequireRole("Administrator", "Workshop Manager")]
    public async Task<IActionResult> AssignUser(int repairJobId, int? assignedUserId)
    {
        var job = await _db.RepairJobs.FindAsync(repairJobId);
        if (job == null) return NotFound();

        AppUser? assignedUser = null;
        if (assignedUserId.HasValue)
        {
            assignedUser = await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == assignedUserId.Value && u.IsActive && u.Role == "Technician");
            if (assignedUser == null)
            {
                TempData["Error"] = "Choose a valid active technician.";
                return RedirectToAction(nameof(Details), new { id = repairJobId });
            }
        }

        var oldUser = string.IsNullOrWhiteSpace(job.AssignedTechnician) ? "Unassigned" : job.AssignedTechnician;
        job.AssignedTechnicianUserId = assignedUser?.Id;
        job.AssignedTechnician = assignedUser?.FullName ?? "";

        await _db.SaveChangesAsync();
        await _audit.LogAsync(repairJobId, $"Assigned technician changed from {oldUser} to {(assignedUser?.FullName ?? "Unassigned")}.");

        TempData["Success"] = "Assigned user updated.";
        return RedirectToAction(nameof(Details), new { id = repairJobId });
    }

    [HttpPost]
    [RequireRole("Administrator", "Workshop Manager", "Receptionist")]
    public async Task<IActionResult> AssignRfid(int repairJobId, string tagCode)
    {
        if (string.IsNullOrWhiteSpace(tagCode))
        {
            TempData["Error"] = "RFID tag code is required.";
            return RedirectToAction(nameof(Details), new { id = repairJobId });
        }

        var result = await _rfidService.AssignTagToRepair(tagCode, repairJobId);
        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Details), new { id = repairJobId });
        }

        await _audit.LogAsync(repairJobId, $"RFID tag assigned: {RfidService.NormalizeTagCode(tagCode)}");
        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = repairJobId });
    }

    [HttpPost]
    [RequireRole("Administrator", "Workshop Manager", "Receptionist")]
    public async Task<IActionResult> RemoveRfid(int repairJobId)
    {
        var job = await _db.RepairJobs.FindAsync(repairJobId);
        if (job == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(job.RfidTagCode))
        {
            await _rfidService.RemoveCurrentAssignment(job.RfidTagCode);
            await _audit.LogAsync(repairJobId, $"RFID tag removed: {job.RfidTagCode}");
        }

        job.RfidTagCode = "";
        await _db.SaveChangesAsync();

        TempData["Success"] = "RFID tag removed from this job.";
        return RedirectToAction(nameof(Details), new { id = repairJobId });
    }

    [HttpPost]
    [RequireRole("Administrator", "Workshop Manager", "Receptionist")]
    public async Task<IActionResult> SaveCollectionSignature(int repairJobId, string customerName, string signatureData)
    {
        var job = await _db.RepairJobs
            .Include(j => j.Documents)
            .Include(j => j.Quote)
                .ThenInclude(q => q!.LineItems)
            .Include(j => j.Checklist)
            .FirstOrDefaultAsync(j => j.Id == repairJobId);
        if (job == null) return NotFound();

        var readyMissing = _completion.GetMissingReadyRequirements(job);
        if (job.Checklist == null || !job.Checklist.IsCompleted) readyMissing.Insert(0, "Checklist");
        if (job.Status != "Ready" || readyMissing.Any())
        {
            TempData["Error"] = "The repair must be Ready with all completion requirements satisfied before collection.";
            return RedirectToAction(nameof(Details), new { id = repairJobId });
        }
        if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(signatureData))
        {
            TempData["Error"] = "Customer name and collection signature are required.";
            return RedirectToAction(nameof(Details), new { id = repairJobId });
        }
        if (!TryDecodePngSignature(signatureData, out var bytes))
        {
            TempData["Error"] = "The collection signature data is invalid or too large.";
            return RedirectToAction(nameof(Details), new { id = repairJobId });
        }
        customerName = customerName.Trim();
        if (customerName.Length > 200) customerName = customerName[..200];
        var uploads = Path.Combine(_env.WebRootPath, "uploads", repairJobId.ToString(), "signatures");
        Directory.CreateDirectory(uploads);
        var fileName = $"collection_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}.png";
        await System.IO.File.WriteAllBytesAsync(Path.Combine(uploads, fileName), bytes);
        job.CollectionCustomerName = customerName;
        job.CollectionSignaturePath = $"/uploads/{repairJobId}/signatures/{fileName}";
        job.CollectedAt = DateTime.Now;
        job.Status = "Collected";
        job.CompletedAt = DateTime.Now;
        _db.JobTimelineEntries.Add(new JobTimelineEntry
        {
            RepairJobId = repairJobId,
            EntryType = "Status Update",
            Message = "Vehicle collected by client.",
            CreatedBy = HttpContext.Session.GetString("UserName") ?? "System",
            IsClientVisible = true,
            CreatedAt = DateTime.Now
        });
        await _db.SaveChangesAsync();
        await _audit.LogAsync(repairJobId, "Vehicle collection signature saved.");
        return RedirectToAction(nameof(Details), new { id = repairJobId });
    }

    private List<string> GetMissingReadyRequirements(RepairJob job)
    {
        var missing = new List<string>();

        if (job.Checklist == null || !job.Checklist.IsCompleted)
            missing.Add("Checklist");

        missing.AddRange(_completion.GetMissingReadyRequirements(job));

        return missing;
    }
    
    private async Task LoadLists()
    {
        ViewBag.VehicleId = new SelectList(
            await _db.Vehicles
                .Include(v => v.Client)
                .OrderBy(v => v.RegNumber)
                .Select(v => new
                {
                    v.Id,
                    Label = v.RegNumber + " - " + v.Client!.FullName
                })
                .ToListAsync(),
            "Id",
            "Label"
        );

        ViewBag.Statuses = AllStatuses;

        ViewBag.AppUsers = await _db.AppUsers
            .Where(u => u.IsActive && u.Role == "Technician")
            .OrderBy(u => u.FullName)
            .ToListAsync();

        ViewBag.AvailableRfidTags = await _db.RfidTags
            .Where(t => t.IsActive && t.CurrentRepairJobId == null)
            .OrderBy(t => t.TagCode)
            .ToListAsync();
    }

    public async Task<IActionResult> QuotePdf(int id)
    {
        var job = await _db.RepairJobs
            .Include(j => j.Client)
            .Include(j => j.Vehicle)
            .Include(j => j.Quote)
                .ThenInclude(q => q!.LineItems)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null) return NotFound();
        ViewBag.CompanySettings = _companySettings.GetSettings();
        return View(job);
    }

    public async Task<IActionResult> ChecklistPdf(int id)
    {
        var job = await _db.RepairJobs
            .Include(j => j.Client)
            .Include(j => j.Vehicle)
            .Include(j => j.Checklist)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null) return NotFound();
        ViewBag.CompanySettings = _companySettings.GetSettings();

        ViewBag.ChecklistItems = await _db.ChecklistItems
            .Where(x => x.RepairJobId == id)
            .OrderBy(x => x.Section)
            .ThenBy(x => x.ItemName)
            .ToListAsync();

        return View(job);
    }
    [RequireRole("Administrator", "Workshop Manager", "Receptionist")]
    public async Task<IActionResult> Quote(int id)
    {
        var job = await _db.RepairJobs
            .Include(j => j.Client)
            .Include(j => j.Vehicle)
            .Include(j => j.Quote)
                .ThenInclude(q => q!.LineItems)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null) return NotFound();
        ViewBag.CompanySettings = _companySettings.GetSettings();
        return View(job);
    }

    public async Task<IActionResult> Checklist(int id)
    {
        var job = await _db.RepairJobs
            .Include(j => j.Client)
            .Include(j => j.Vehicle)
            .Include(j => j.Checklist)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null) return NotFound();
        ViewBag.CompanySettings = _companySettings.GetSettings();

        await _checklistTemplate.EnsureChecklistItemsAsync(job.Id);

        ViewBag.ChecklistItems = await _db.ChecklistItems
            .Where(x => x.RepairJobId == job.Id)
            .OrderBy(x => x.Section)
            .ThenBy(x => x.ItemName)
            .ToListAsync();

        return View(job);
    }

    [RequireRole("Administrator", "Workshop Manager", "Receptionist")]
    public IActionResult Damage(int id)
    {
        TempData["Info"] = "The inspection screen has been retired. Parts are managed in Inventory.";
        return RedirectToAction(nameof(Inventory), new { id });
    }

    [RequireRole("Administrator", "Workshop Manager", "Receptionist")]
    public async Task<IActionResult> Documents(int id)
    {
        var job = await _db.RepairJobs
            .Include(j => j.Client)
            .Include(j => j.Vehicle)
            .Include(j => j.Documents)
            .Include(j => j.Quote)
                .ThenInclude(q => q!.LineItems)
            .Include(j => j.Checklist)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null) return NotFound();

        return View(job);
    }

    public async Task<IActionResult> Photos(int id)
    {
        var job = await _db.RepairJobs
            .Include(j => j.Client)
            .Include(j => j.Vehicle)
            .Include(j => j.Photos)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null) return NotFound();

        return View(job);
    }

    public async Task<IActionResult> Timeline(int id)
    {
        var job = await _db.RepairJobs
            .Include(j => j.Client)
            .Include(j => j.Vehicle)
            .Include(j => j.TimelineEntries)
            .Include(j => j.AuditLogs)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null) return NotFound();

        return View(job);
    }

    
    public async Task<IActionResult> Inventory(int id)
    {
        var job = await _db.RepairJobs
            .Include(j => j.Client)
            .Include(j => j.Vehicle)
            .Include(j => j.Parts)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null) return NotFound();

        return View(job);
    }
    private static bool TryDecodePngSignature(string? signatureData, out byte[] bytes)
    {
        bytes = [];
        const string prefix = "data:image/png;base64,";
        if (string.IsNullOrWhiteSpace(signatureData) ||
            !signatureData.StartsWith(prefix, StringComparison.Ordinal) ||
            signatureData.Length > 14_000_000)
            return false;

        try
        {
            bytes = Convert.FromBase64String(signatureData[prefix.Length..]);
            return bytes.Length is > 8 and <= 10 * 1024 * 1024 &&
                   bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
                   bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }

    private async Task<bool> TechnicianCanAccessJobAsync(int repairJobId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return false;
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "";
        return await _db.RepairJobs.AnyAsync(j =>
            j.Id == repairJobId &&
            (j.AssignedTechnicianUserId == userId ||
             (j.AssignedTechnicianUserId == null && j.AssignedTechnician == userName &&
              !_db.AppUsers.Any(u => u.IsActive && u.Id != userId && u.FullName == userName))));
    }

    private decimal ReadDecimal(string key, decimal fallback = 0)
    {
        return decimal.TryParse(Request.Form[key], out var value) ? value : fallback;
    }

    private static decimal ReadListDecimal(List<string?> values, int index)
    {
        return index < values.Count && decimal.TryParse(values[index], out var value) ? value : 0;
    }

    private static string GetAt(List<string?> values, int index)
    {
        return index < values.Count ? values[index] ?? "" : "";
    }

    
    private async Task SyncQuotedPartsToInventoryAsync(int repairJobId, List<QuoteLineItem> quotedPartItems)
    {
        if (quotedPartItems.Count == 0) return;

        var existingParts = await _db.Parts
            .Where(p => p.RepairJobId == repairJobId)
            .ToListAsync();

        foreach (var lineItem in quotedPartItems)
        {
            var match = existingParts.FirstOrDefault(p =>
                p.Description.Equals(lineItem.Description, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                
                match.Quantity = (int)Math.Max(1, lineItem.Quantity);
                match.QuotedUnitPrice = lineItem.UnitPrice;
                match.TotalQuotedPrice = lineItem.Value;
                continue;
            }

            var part = new Part
            {
                RepairJobId = repairJobId,
                PartNumber = lineItem.Code ?? "",
                Description = lineItem.Description,
                Quantity = (int)Math.Max(1, lineItem.Quantity),
                QuotedUnitPrice = lineItem.UnitPrice,
                TotalQuotedPrice = lineItem.Value,
                Status = "Required", 
                CreatedAt = DateTime.Now
            };
            _db.Parts.Add(part);
            existingParts.Add(part);
        }
    }

}
/*

Szimek. 2025. Signature Pad – HTML5 canvas based smooth signature drawing [Source code].
Available at:https://github.com/szimek/signature_pad
[Accessed 14 July 2026].


Callmephil. 2025. Quotation generator: Generate a quotation and allow you to download it as PDF [Source code].
Available at:https://gist.github.com/callmephil/bcdc21018c65b61965641781cc0846cb
[Accessed 14 July 2026].


ZXing. 2026. ZXing Browser – barcode and QR code scanning from a camera [Source code].
Available at:
https://github.com/zxing-js/browser
[Accessed 14 July 2026].

Microsoft. 2026. Upload files in ASP.NET Core [Source code].
Available at:https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0
[Accessed 14 July 2026].

*/