using System.ComponentModel.DataAnnotations;

namespace AutoTracker.Models;

public class Company
{
    public int Id { get; set; }
    [Required] public string Name { get; set; } = "Company";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
