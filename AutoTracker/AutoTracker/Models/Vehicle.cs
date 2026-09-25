using System.ComponentModel.DataAnnotations;

namespace AutoTracker.Models;

public class Vehicle
{
    public int Id { get; set; }
    [Required, StringLength(30)] public string RegNumber { get; set; } = string.Empty;
    [StringLength(80)] public string VehicleType { get; set; } = string.Empty;
    [StringLength(80)] public string Make { get; set; } = string.Empty;
    [StringLength(80)] public string ModelName { get; set; } = string.Empty;
    [StringLength(50)] public string VinNumber { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public Client? Client { get; set; }
    public ICollection<RepairJob> RepairJobs { get; set; } = new List<RepairJob>();
}
