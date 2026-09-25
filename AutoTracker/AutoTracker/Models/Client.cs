using System.ComponentModel.DataAnnotations;

namespace AutoTracker.Models;

public class Client
{
    public int Id { get; set; }
    [Required, StringLength(120)] public string FullName { get; set; } = string.Empty;
    [StringLength(40)] public string Phone { get; set; } = string.Empty;
    [EmailAddress, StringLength(120)] public string Email { get; set; } = string.Empty;
    [StringLength(250)] public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<RepairJob> RepairJobs { get; set; } = new List<RepairJob>();
}
