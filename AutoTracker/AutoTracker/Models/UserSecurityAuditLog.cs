namespace AutoTracker.Models;

public class UserSecurityAuditLog
{
    public int Id { get; set; }
    public int ActorUserId { get; set; }
    public int TargetUserId { get; set; }
    public string Action { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
