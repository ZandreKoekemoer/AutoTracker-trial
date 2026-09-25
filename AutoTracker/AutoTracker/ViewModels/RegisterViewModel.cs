using System.ComponentModel.DataAnnotations;

namespace AutoTracker.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        public string FullName { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, MinLength(8)]
        public string Password { get; set; } = "";

        [Required]
        public string Role { get; set; } = "Technician";
    }
}