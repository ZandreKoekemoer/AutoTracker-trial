namespace AutoTracker.Models;

public class Checklist
{
    public int Id { get; set; }
    public int RepairJobId { get; set; }
    public RepairJob? RepairJob { get; set; }
    public bool VehicleReceived { get; set; }
    public bool DamagePhotosTaken { get; set; }
    public bool AuthorizationConfirmed { get; set; }
    public bool PartsChecked { get; set; }
    public bool RepairCompleted { get; set; }
    public bool QualityChecked { get; set; }
    public bool FinalPhotosTaken { get; set; }
    public bool ClientNotified { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
