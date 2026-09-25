namespace AutoTracker.Models;

public class QuoteLineItem
{
    public int Id { get; set; }
    public int RepairJobId { get; set; }
    public RepairJob? RepairJob { get; set; }
    public int QuoteId { get; set; }
    public Quote? Quote { get; set; }
    public string Section { get; set; } = "Parts"; // Parts, Labour, Paint, StripAndAssemble
    public string Method { get; set; } = "";
    public string Description { get; set; } = "";
    public string Code { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Hours { get; set; }
    public decimal Panels { get; set; }
    public decimal Value { get; set; }
    public int SortOrder { get; set; }
}
