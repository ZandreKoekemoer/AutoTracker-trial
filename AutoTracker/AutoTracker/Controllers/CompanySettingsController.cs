using AutoTracker.Filters;
using AutoTracker.Models;
using AutoTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoTracker.Controllers
{
    [RequireRole("Administrator")]
public class CompanySettingsController : Controller
    {
        private readonly CompanySettingService _service;

        public CompanySettingsController(CompanySettingService service)
        {
            _service = service;
        }

        public IActionResult Index()
        {
            return View(_service.GetSettings());
        }

        [HttpPost]
        public IActionResult Index(
            CompanySetting model,
            IFormFile? logoFile,
            IFormFile? companySignatureFile,
            string? CompanySignatureData)
        {
            ModelState.Remove("LogoPath");
            ModelState.Remove("CompanySignaturePath");

            if (string.IsNullOrWhiteSpace(model.CompanyName))
                model.CompanyName = "KeyTrack Pro";

            if (string.IsNullOrWhiteSpace(model.QuotePrefix))
                model.QuotePrefix = "EST";

            if (string.IsNullOrWhiteSpace(model.InvoicePrefix))
                model.InvoicePrefix = "INV";

            if (string.IsNullOrWhiteSpace(model.QuoteFooterText))
                model.QuoteFooterText = "TERMS: STRICTLY CASH. Quote valid for 30 days.";

            if (string.IsNullOrWhiteSpace(model.ChecklistFooterText))
                model.ChecklistFooterText = "Checklist completed and verified by the workshop.";

            _service.UpdateSettings(model, logoFile, companySignatureFile, CompanySignatureData);

            TempData["Success"] = "Company settings saved successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}