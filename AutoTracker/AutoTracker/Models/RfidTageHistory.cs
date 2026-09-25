namespace AutoTracker.Models;

public class RfidTagHistory
{
    public int Id { get; set; }

    public int RfidTagId { get; set; }

    public RfidTag? RfidTag { get; set; }

    public int RepairJobId { get; set; }

    public RepairJob? RepairJob { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.Now;

    public DateTime? RemovedAt { get; set; }

    public string Notes { get; set; } = "";
}