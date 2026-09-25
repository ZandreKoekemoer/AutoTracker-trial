using System.ComponentModel.DataAnnotations;

namespace AutoTracker.Models;

public class Part
{
    public int Id { get; set; }

    public int RepairJobId { get; set; }
    public RepairJob? RepairJob { get; set; }

    public int? VehicleDamageItemId { get; set; }
    public VehicleDamageItem? VehicleDamageItem { get; set; }

    [Required]
    public string PartNumber { get; set; } = "";

    [Required]
    public string Description { get; set; } = "";

    public int Quantity { get; set; } = 1;

    public decimal QuotedUnitPrice { get; set; }
    public decimal ActualUnitCost { get; set; }

    public decimal TotalQuotedPrice { get; set; }
    public decimal TotalActualCost { get; set; }

    public string Supplier { get; set; } = "";

    public string Status { get; set; } = "Required";

    public bool CriticalPart { get; set; }

    public bool InvoiceUploaded { get; set; }

    public string Notes { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}