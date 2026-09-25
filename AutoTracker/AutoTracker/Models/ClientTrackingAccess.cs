namespace AutoTracker.Models;

public class ClientTrackingAccess
{
    public int Id { get; set; }
    public int RepairJobId { get; set; }
    public RepairJob? RepairJob { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; }
    public DateTime? LastAccessedAtUtc { get; set; }
}
