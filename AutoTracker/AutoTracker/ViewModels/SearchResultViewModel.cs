using AutoTracker.Models;

namespace AutoTracker.ViewModels;

public class SearchResultViewModel
{
    public string SearchTerm { get; set; } = "";

    public List<Client> Clients { get; set; } = new();
    public List<Vehicle> Vehicles { get; set; } = new();
    public List<RepairJob> RepairJobs { get; set; } = new();
    public List<RfidTag> RfidTags { get; set; } = new();
}