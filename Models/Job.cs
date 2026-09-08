using System.ComponentModel.DataAnnotations;
using static System.Net.Mime.MediaTypeNames;

namespace WorkAt.Models
{
    public class Job
    {
        public int JobId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public string? Requirements { get; set; }

        [StringLength(150)]
        public string? Location { get; set; }

        [StringLength(100)]
        public string? Salary { get; set; }

        [StringLength(50)]
        public string? EmploymentType { get; set; }

        public DateTime PostedDate { get; set; } = DateTime.UtcNow;

        public DateTime? Deadline { get; set; }

        // Company that posted the job
        public int CompanyId { get; set; }

        public Company? Company { get; set; }

        // Applications submitted for this job
        public ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}