using System.ComponentModel.DataAnnotations;

namespace WorkAt.Models
{
    public class Skill
    {
        public int SkillId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        // A skill can belong to multiple resumes
        public ICollection<ResumeSkill> ResumeSkills { get; set; } = new List<ResumeSkill>();
    }
}