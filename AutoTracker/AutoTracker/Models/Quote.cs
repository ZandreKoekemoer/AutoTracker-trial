namespace AutoTracker.Models;

public class Quote
{
    public int Id { get; set; }

    public int RepairJobId { get; set; }
    public RepairJob? RepairJob { get; set; }

    public string QuoteNumber { get; set; } = "";
    public DateTime QuoteDate { get; set; } = DateTime.Now;

    public decimal Labour { get; set; }
    public decimal Paint { get; set; }
    public decimal Parts { get; set; }
    public decimal StripAndAssemble { get; set; }
    public decimal PanelBeating { get; set; }
    public decimal Polishing { get; set; }
    public decimal Consumables { get; set; }
    public decimal Sublet { get; set; }
    public decimal Discount { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Vat { get; set; }
    public decimal Total { get; set; }

    public string Notes { get; set; } = "";
    public string Terms { get; set; } = "Quote valid for 30 days. Repairs only start once authorized.";

    public bool IsCompleted { get; set; }

    public string InvoiceNumber { get; set; } = "";
    public DateTime? InvoiceDate { get; set; }
    public ICollection<QuoteLineItem> LineItems { get; set; } = new List<QuoteLineItem>();

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}