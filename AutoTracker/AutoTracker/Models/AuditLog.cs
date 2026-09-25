namespace AutoTracker.Models;

public class JobAuditLog
{
    public int Id { get; set; }

    public int RepairJobId { get; set; }

    public RepairJob? RepairJob { get; set; }

    public string Action { get; set; } = "";

    public string UserName { get; set; } = "System";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}