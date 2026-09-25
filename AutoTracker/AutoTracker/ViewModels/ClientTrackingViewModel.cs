namespace AutoTracker.ViewModels;

public class ClientTrackingViewModel
{
    public string Token { get; set; } = "";
    public string RepairReference { get; set; } = "";
    public string RegistrationNumber { get; set; } = "";
    public string VehicleDescription { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? EstimatedCompletionDate { get; set; }
    public DateTime LastUpdated { get; set; }
    public string WorkshopName { get; set; } = "AutoTracker";
    public string WorkshopPhone { get; set; } = "";
    public string WorkshopEmail { get; set; } = "";
    public List<ClientTrackingUpdateViewModel> Updates { get; set; } = [];
    public List<ClientTrackingPhotoViewModel> Photos { get; set; } = [];
}

public class ClientTrackingUpdateViewModel
{
    public string Message { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class ClientTrackingPhotoViewModel
{
    public int Id { get; set; }
    public string Notes { get; set; } = "";
    public DateTime UploadedAt { get; set; }
}
