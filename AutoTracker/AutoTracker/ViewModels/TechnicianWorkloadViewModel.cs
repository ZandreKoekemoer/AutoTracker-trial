using AutoTracker.Models;

namespace AutoTracker.ViewModels;

public class TechnicianWorkloadViewModel
{
    public List<TechnicianWorkloadItem> Technicians { get; set; } = new();
    public List<RepairJob> UnassignedJobs { get; set; } = new();
}

public class TechnicianWorkloadItem
{
    public string TechnicianName { get; set; } = "";
    public List<RepairJob> Jobs { get; set; } = new();
}