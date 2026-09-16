using System.ComponentModel.DataAnnotations;

namespace WorkAt.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select an account type.")]
        [Display(Name = "Account Type")]
        public string AccountType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Display(Name = "Phone Number")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        [RegularExpression(@"^(\+?[0-9\s\-\(\)]{7,20})$", ErrorMessage = "Please enter a valid phone number (7 to 20 digits/standard formatting).")]
        public string Phone { get; set; } = string.Empty;

        // Company specific information
        [Display(Name = "Company Name")]
        [StringLength(150, ErrorMessage = "Company name cannot exceed 150 characters.")]
        public string? CompanyName { get; set; }

        [Display(Name = "Company Description")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        [Display(Name = "Company Website")]
        [StringLength(250, ErrorMessage = "Website URL cannot exceed 250 characters.")]
        [Url(ErrorMessage = "Please enter a valid website URL (e.g. https://example.com).")]
        public string? Website { get; set; }

        // Job seeker specific information
        [Display(Name = "First Name")]
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
        public string? LastName { get; set; }

        // Shared address information
        [Display(Name = "Address")]
        [StringLength(250, ErrorMessage = "Address cannot exceed 250 characters.")]
        public string? Address { get; set; }
    }
}