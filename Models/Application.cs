using System.ComponentModel.DataAnnotations;

namespace WorkAt.Models
{
    public class Application
    {
        public int ApplicationId { get; set; }

        public DateTime AppliedDate { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        // Job being applied for
        public int JobId { get; set; }

        public Job? Job { get; set; }

        // Job seeker who submitted the application
        public int JobSeekerId { get; set; }

        public JobSeeker? JobSeeker { get; set; }

        // Optional company feedback for this application
        public ApplicationFeedback? Feedback { get; set; }

        // PDF Resume Attachment
        [StringLength(500)]
        public string? ResumePath { get; set; }

        [StringLength(250)]
        public string? ResumeFileName { get; set; }
    }
}