using System.ComponentModel.DataAnnotations;
using static System.Net.Mime.MediaTypeNames;

namespace WorkAt.Models
{
    public class JobSeeker
    {
        public int JobSeekerId { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(250)]
        public string? Address { get; set; }

        // Link this profile to its Identity user
        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // A job seeker can submit many applications
        public ICollection<Application> Applications { get; set; } = new List<Application>();

        // A job seeker has one resume
        public Resume? Resume { get; set; }
    }
}