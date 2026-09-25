namespace AutoTracker.Models
{
    public class JobDocument
    {
        public int Id { get; set; }

        public int RepairJobId { get; set; }
        public RepairJob? RepairJob { get; set; }

        public string DocumentType { get; set; } = "";

        public string FileName { get; set; } = "";

        public string FilePath { get; set; } = "";

        public bool IsCompleted { get; set; }

        public decimal CostAmount { get; set; }
        public string CostCategory { get; set; } = "";
        public bool ManualCostOverride { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}