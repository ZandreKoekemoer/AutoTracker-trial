using System.ComponentModel.DataAnnotations;

namespace AutoTracker.Models
{
    public class CompanySetting
    {
        public int Id { get; set; }
        [Required]
        public string CompanyName { get; set; } = "";
        public string LogoPath { get; set; } = "";
        public string Address { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Website { get; set; } = "";
        public string VatNumber { get; set; } = "";
        public string RegistrationNumber { get; set; } = "";
        public string CKNumber { get; set; } = "";
        public decimal VatPercentage { get; set; } = 15;
        public decimal LabourRatePerHour { get; set; } = 0;
        public decimal PaintRatePerPanel { get; set; } = 0;
        public decimal StripAssembleRatePerHour { get; set; } = 0;
        public decimal MechanicalRatePerHour { get; set; } = 0;
        public decimal PanelBeatingRatePerHour { get; set; } = 0;
        public decimal DefaultSundries { get; set; } = 0;
        public decimal DefaultConsumables { get; set; } = 0;
        public decimal DefaultFreight { get; set; } = 0;
        public decimal DefaultWasteDisposal { get; set; } = 0;
        public string QuotePrefix { get; set; } = "EST";
        public int LastQuoteNumber { get; set; } = 0;
        public string InvoicePrefix { get; set; } = "INV";
        public int LastInvoiceNumber { get; set; } = 0;
        public string QuoteFooterText { get; set; } = "TERMS: STRICTLY CASH. NB: PRICES OF SPARES SUBJECT TO FLUCTUATION. Quote valid for 30 days.";
        public string ChecklistFooterText { get; set; } = "Checklist completed and verified by the workshop.";
        public string BankingDetails { get; set; } = "";
        public string CompanySignatureName { get; set; } = "";
        public string CompanySignaturePath { get; set; } = "";
        public string ThemeMode { get; set; } = "dark";
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
