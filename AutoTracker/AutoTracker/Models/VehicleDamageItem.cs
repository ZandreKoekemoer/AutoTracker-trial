namespace AutoTracker.Models;

public class VehicleDamageItem
{
    public int Id { get; set; }

    public int RepairJobId { get; set; }
    public RepairJob? RepairJob { get; set; }

    public string ViewSide { get; set; } = "";
    public string BodyPart { get; set; } = "";

    public string Condition { get; set; } = "Not Inspected";
    public string RepairType { get; set; } = "None";

    public decimal LabourHours { get; set; }
    public decimal PaintHours { get; set; }
    public decimal EstimatedCost { get; set; }

    public string Notes { get; set; } = "";

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public ICollection<Part> Parts { get; set; } = new List<Part>();
}