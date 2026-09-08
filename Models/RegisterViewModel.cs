using System.ComponentModel.DataAnnotations;

namespace WorkAt.Models
{
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Account Type")]
        public string AccountType { get; set; } = string.Empty;

        // Company information
        [Display(Name = "Company Name")]
        [StringLength(150)]
        public string? CompanyName { get; set; }

        // Job seeker information
        [Display(Name = "First Name")]
        [StringLength(100)]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        [StringLength(100)]
        public string? LastName { get; set; }
    }
}