using System.ComponentModel.DataAnnotations;

namespace WorkAt.Models
{
    public class ApplicationFeedback
    {
        [Key]
        public int FeedbackId { get; set; }

        public int ApplicationId { get; set; }

        public Application? Application { get; set; }

        [Required(ErrorMessage = "Feedback text is required.")]
        public string FeedbackText { get; set; } = string.Empty;

        public DateTime FeedbackDate { get; set; } = DateTime.UtcNow;
    }
}
