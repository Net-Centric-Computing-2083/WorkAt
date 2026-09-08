using System.ComponentModel.DataAnnotations;

namespace WorkAt.Models
{
    public class Resume
    {
        public int ResumeId { get; set; }

        [StringLength(2000)]
        public string? Summary { get; set; }

        [StringLength(2000)]
        public string? Education { get; set; }

        [StringLength(2000)]
        public string? Experience { get; set; }

        // Resume belongs to one job seeker
        public int JobSeekerId { get; set; }

        public JobSeeker? JobSeeker { get; set; }

        // A resume can contain multiple skills
        public ICollection<ResumeSkill> ResumeSkills { get; set; } = new List<ResumeSkill>();
    }
}