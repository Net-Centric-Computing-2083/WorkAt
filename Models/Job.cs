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

        // Computed property: Checks if the job application deadline has passed
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsDeadlinePassed
        {
            get
            {
                if (!Deadline.HasValue) return false;
                var now = DateTime.UtcNow;
                if (Deadline.Value.TimeOfDay == TimeSpan.Zero)
                {
                    // If deadline is date-only (00:00:00), it expires after that date ends
                    return now.Date > Deadline.Value.Date;
                }
                return now > Deadline.Value;
            }
        }
    }
}