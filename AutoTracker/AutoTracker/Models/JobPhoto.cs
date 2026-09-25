namespace AutoTracker.Models;

public class JobPhoto
{
    public int Id { get; set; }

    public int RepairJobId { get; set; }
    public RepairJob? RepairJob { get; set; }

    public string Category { get; set; } = "";

    public string FileName { get; set; } = "";

    public string FilePath { get; set; } = "";

    public string Notes { get; set; } = "";
    public bool IsClientVisible { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.Now;
}