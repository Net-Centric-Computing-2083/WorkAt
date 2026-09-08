using System.ComponentModel.DataAnnotations;

namespace WorkAt.Models
{
    public class Company
    {
        public int CompanyId { get; set; }

        [Required]
        [StringLength(150)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(250)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(250)]
        public string? Website { get; set; }

        // Link this company profile to its Identity user
        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }

        // A company can post many jobs
        public ICollection<Job> Jobs { get; set; } = new List<Job>();
    }
}