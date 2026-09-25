using AutoTracker.Data;
using AutoTracker.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AutoTracker.Services
{
    public class CompanySettingService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CompanySettingService(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public CompanySetting GetSettings()
        {
            var settings = _context.CompanySettings.FirstOrDefault();

            if (settings == null)
            {
                settings = new CompanySetting
                {
                    CompanyName = "KeyTrack Pro",
                    VatPercentage = 15,
                    QuotePrefix = "EST",
                    InvoicePrefix = "INV",
                    QuoteFooterText = "TERMS: STRICTLY CASH. NB: PRICES OF SPARES SUBJECT TO FLUCTUATION. Quote valid for 30 days.",
                    ChecklistFooterText = "Checklist completed and verified by the workshop.",
                    ThemeMode = "dark",
                    UpdatedAt = DateTime.Now
                };

                _context.CompanySettings.Add(settings);
                _context.SaveChanges();
            }

            return settings;
        }

        public void UpdateSettings(
            CompanySetting model,
            IFormFile? logoFile,
            IFormFile? companySignatureFile,
            string? companySignatureData)
        {
            var settings = GetSettings();

            settings.CompanyName = model.CompanyName ?? "";
            settings.Address = model.Address ?? "";
            settings.Phone = model.Phone ?? "";
            settings.Email = model.Email ?? "";
            settings.Website = model.Website ?? "";
            settings.VatNumber = model.VatNumber ?? "";
            settings.RegistrationNumber = model.RegistrationNumber ?? "";
            settings.CKNumber = model.CKNumber ?? "";

            settings.VatPercentage = model.VatPercentage;
            settings.LabourRatePerHour = model.LabourRatePerHour;
            settings.PaintRatePerPanel = model.PaintRatePerPanel;
            settings.StripAssembleRatePerHour = model.StripAssembleRatePerHour;
            settings.MechanicalRatePerHour = model.MechanicalRatePerHour;
            settings.PanelBeatingRatePerHour = model.PanelBeatingRatePerHour;

            settings.DefaultSundries = model.DefaultSundries;
            settings.DefaultConsumables = model.DefaultConsumables;
            settings.DefaultFreight = model.DefaultFreight;
            settings.DefaultWasteDisposal = model.DefaultWasteDisposal;

            settings.QuotePrefix = string.IsNullOrWhiteSpace(model.QuotePrefix) ? "EST" : model.QuotePrefix;
            settings.InvoicePrefix = string.IsNullOrWhiteSpace(model.InvoicePrefix) ? "INV" : model.InvoicePrefix;

            settings.QuoteFooterText = string.IsNullOrWhiteSpace(model.QuoteFooterText)
                ? "TERMS: STRICTLY CASH. Quote valid for 30 days."
                : model.QuoteFooterText;

            settings.ChecklistFooterText = string.IsNullOrWhiteSpace(model.ChecklistFooterText)
                ? "Checklist completed and verified by the workshop."
                : model.ChecklistFooterText;

            settings.BankingDetails = model.BankingDetails ?? "";
            settings.CompanySignatureName = model.CompanySignatureName ?? "";
            settings.ThemeMode = string.IsNullOrWhiteSpace(model.ThemeMode) ? "dark" : model.ThemeMode;
            settings.UpdatedAt = DateTime.Now;

            if (logoFile != null && logoFile.Length > 0)
            {
                settings.LogoPath = SaveFile(logoFile, "logo");
            }

            if (!string.IsNullOrWhiteSpace(companySignatureData))
            {
                settings.CompanySignaturePath = SaveBase64Signature(companySignatureData);
            }
            else if (companySignatureFile != null && companySignatureFile.Length > 0)
            {
                settings.CompanySignaturePath = SaveFile(companySignatureFile, "signature");
            }

            _context.SaveChanges();
        }

        public string NextQuoteNumber() => NextNumber(isQuote: true);

        public string NextInvoiceNumber() => NextNumber(isQuote: false);

        private string NextNumber(bool isQuote)
        {
            GetSettings();
            var connection = _context.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;
            if (shouldClose) connection.Open();
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = isQuote
                    ? "UPDATE CompanySettings SET LastQuoteNumber = LastQuoteNumber + 1 WHERE Id = (SELECT Id FROM CompanySettings ORDER BY Id LIMIT 1) RETURNING LastQuoteNumber, QuotePrefix;"
                    : "UPDATE CompanySettings SET LastInvoiceNumber = LastInvoiceNumber + 1 WHERE Id = (SELECT Id FROM CompanySettings ORDER BY Id LIMIT 1) RETURNING LastInvoiceNumber, InvoicePrefix;";
                using var reader = command.ExecuteReader();
                if (!reader.Read()) throw new InvalidOperationException("Company settings are unavailable.");
                var number = reader.GetInt32(0);
                var prefix = reader.GetString(1);

                var tracked = _context.ChangeTracker.Entries<CompanySetting>().FirstOrDefault();
                if (tracked != null)
                {
                    var property = tracked.Property(isQuote ? nameof(CompanySetting.LastQuoteNumber) : nameof(CompanySetting.LastInvoiceNumber));
                    property.CurrentValue = number;
                    property.OriginalValue = number;
                }
                return $"{prefix}-{number:0000}";
            }
            finally
            {
                if (shouldClose) connection.Close();
            }
        }

        private string SaveFile(IFormFile file, string purpose)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "company");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{purpose}_{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            file.CopyTo(stream);

            return "/uploads/company/" + fileName;
        }

        private string SaveBase64Signature(string signatureData)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "company");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var base64 = signatureData.Replace("data:image/png;base64,", "");
            var bytes = Convert.FromBase64String(base64);

            var fileName = $"company_signature_{Guid.NewGuid():N}.png";
            var filePath = Path.Combine(uploadsFolder, fileName);

            File.WriteAllBytes(filePath, bytes);

            return "/uploads/company/" + fileName;
        }
    }
}