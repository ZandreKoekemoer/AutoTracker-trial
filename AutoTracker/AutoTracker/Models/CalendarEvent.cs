namespace AutoTracker.Models;

public class CalendarEvent
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime StartTime { get; set; } = DateTime.Now;
    public DateTime? EndTime { get; set; }
    public int? RepairJobId { get; set; }
    public RepairJob? RepairJob { get; set; }
    public string EventType { get; set; } = "Manual";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
