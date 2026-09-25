namespace AutoTracker.Models;

public class RfidTag
{
    public int Id { get; set; }

    public string TagCode { get; set; } = "";

    public int? CurrentRepairJobId { get; set; }

    public RepairJob? CurrentRepairJob { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<RfidTagHistory> History { get; set; } = new List<RfidTagHistory>();
}