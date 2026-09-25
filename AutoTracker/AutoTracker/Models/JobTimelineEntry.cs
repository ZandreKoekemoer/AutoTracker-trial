namespace AutoTracker.Models;

public class JobTimelineEntry
{
    public int Id { get; set; }

    public int RepairJobId { get; set; }
    public RepairJob? RepairJob { get; set; }

    public string EntryType { get; set; } = "Note";

    public string Message { get; set; } = "";

    public string CreatedBy { get; set; } = "System";
    public bool IsClientVisible { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}