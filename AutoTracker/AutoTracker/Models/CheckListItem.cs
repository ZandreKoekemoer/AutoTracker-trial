namespace AutoTracker.Models;

public class ChecklistItem
{
    public int Id { get; set; }

    public int RepairJobId { get; set; }
    public RepairJob? RepairJob { get; set; }

    public string Section { get; set; } = "";
    public string ItemName { get; set; } = "";

    public string Condition { get; set; } = "Not Checked";
    public string Notes { get; set; } = "";

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}