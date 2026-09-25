using System.ComponentModel.DataAnnotations;

namespace AutoTracker.Models
{
    public class AppUser
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string PasswordHash { get; set; } = "";

        [Required]
        public string Role { get; set; } = "Technician";

        public int CompanyId { get; set; } = 1;
        public Company? Company { get; set; }

        public bool IsActive { get; set; } = true;
        public int SessionVersion { get; set; } = 1;
        public DateTime? LastLoginAtUtc { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}