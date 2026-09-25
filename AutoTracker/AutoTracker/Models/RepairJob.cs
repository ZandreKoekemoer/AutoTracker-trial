namespace AutoTracker.Models
{
    public class RepairJob
    {
        public int Id { get; set; }

        public int ClientId { get; set; }
        public Client? Client { get; set; }

        public int VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }

        public string Status { get; set; } = "Booked In";

        public string RfidTagCode { get; set; } = "";

        public string AssignedTechnician { get; set; } = "";
        public int? AssignedTechnicianUserId { get; set; }

        public string Priority { get; set; } = "Normal";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? EstimatedCompletionDate { get; set; }

        public DateTime? CompletedAt { get; set; }

        public string TrackingToken { get; set; } = "";

        public string Notes { get; set; } = "";
        public decimal ActualPartsCost { get; set; }
        public decimal ActualLabourCost { get; set; }
        public decimal ActualPaintCost { get; set; }
        public decimal ActualConsumablesCost { get; set; }
        public decimal ActualSubletCost { get; set; }

        public decimal ActualTotalCost { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitMargin { get; set; }

        public string InternalNotes { get; set; } = "";
        public string CollectionCustomerName { get; set; } = "";
        public string CollectionSignaturePath { get; set; } = "";
        public DateTime? CollectedAt { get; set; }

        public ICollection<JobDocument> Documents { get; set; } = new List<JobDocument>();

        public Quote? Quote { get; set; }

        public Checklist? Checklist { get; set; }
        public ICollection<JobPhoto> Photos { get; set; } = new List<JobPhoto>();
        public ICollection<JobTimelineEntry> TimelineEntries { get; set; } = new List<JobTimelineEntry>();
        public ICollection<JobAuditLog> AuditLogs { get; set; } = new List<JobAuditLog>();
        public ICollection<CustomerSignature> CustomerSignatures { get; set; } = new List<CustomerSignature>();
        public ICollection<VehicleDamageItem> DamageItems { get; set; } = new List<VehicleDamageItem>();
        public ICollection<Part> Parts { get; set; } = new List<Part>();
        public ICollection<ClientTrackingAccess> TrackingAccesses { get; set; } = new List<ClientTrackingAccess>();
    }
}