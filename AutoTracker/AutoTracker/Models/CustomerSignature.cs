namespace AutoTracker.Models;

public class CustomerSignature
{
    public int Id { get; set; }

    public int RepairJobId { get; set; }
    public RepairJob? RepairJob { get; set; }

    public string CustomerName { get; set; } = "";
    public string SignaturePath { get; set; } = "";

    public string SignatureType { get; set; } = "Authorization";

    public DateTime SignedAt { get; set; } = DateTime.Now;
}