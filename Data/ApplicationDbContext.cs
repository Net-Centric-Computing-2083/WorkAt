using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WorkAt.Models;

namespace WorkAt.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<JobSeeker> JobSeekers { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<Resume> Resumes { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<ResumeSkill> ResumeSkills { get; set; }
        public DbSet<ApplicationFeedback> ApplicationFeedbacks { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Application -> ApplicationFeedback (1:0..1)
            builder.Entity<ApplicationFeedback>()
                .HasKey(af => af.FeedbackId);

            builder.Entity<ApplicationFeedback>()
                .HasOne(af => af.Application)
                .WithOne(a => a.Feedback)
                .HasForeignKey<ApplicationFeedback>(af => af.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ApplicationFeedback>()
                .HasIndex(af => af.ApplicationId)
                .IsUnique();

            // ApplicationUser -> Admin (1:1)
            builder.Entity<Admin>()
                .HasOne(a => a.User)
                .WithOne()
                .HasForeignKey<Admin>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ApplicationUser -> Company (1:1)
            builder.Entity<Company>()
                .HasOne(c => c.User)
                .WithOne()
                .HasForeignKey<Company>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ApplicationUser -> JobSeeker (1:1)
            builder.Entity<JobSeeker>()
                .HasOne(js => js.User)
                .WithOne()
                .HasForeignKey<JobSeeker>(js => js.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Company -> Jobs (1:many)
            builder.Entity<Job>()
                .HasOne(j => j.Company)
                .WithMany(c => c.Jobs)
                .HasForeignKey(j => j.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // JobSeeker -> Applications (1:many)
            builder.Entity<Application>()
                .HasOne(a => a.JobSeeker)
                .WithMany(js => js.Applications)
                .HasForeignKey(a => a.JobSeekerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Job -> Applications (1:many)
            builder.Entity<Application>()
                .HasOne(a => a.Job)
                .WithMany(j => j.Applications)
                .HasForeignKey(a => a.JobId)
                .OnDelete(DeleteBehavior.Restrict);

            // JobSeeker -> Resume (1:1)
            builder.Entity<Resume>()
                .HasOne(r => r.JobSeeker)
                .WithOne(js => js.Resume)
                .HasForeignKey<Resume>(r => r.JobSeekerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Resume -> Skill (many:many through ResumeSkill)
            builder.Entity<ResumeSkill>()
                .HasKey(rs => new { rs.ResumeId, rs.SkillId });

            builder.Entity<ResumeSkill>()
                .HasOne(rs => rs.Resume)
                .WithMany(r => r.ResumeSkills)
                .HasForeignKey(rs => rs.ResumeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ResumeSkill>()
                .HasOne(rs => rs.Skill)
                .WithMany(s => s.ResumeSkills)
                .HasForeignKey(rs => rs.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
            // Unique Phone on Companies where Phone is not null
            builder.Entity<Company>()
                .HasIndex(c => c.Phone)
                .IsUnique()
                .HasFilter("[Phone] IS NOT NULL");

            // Unique Phone on JobSeekers where Phone is not null
            builder.Entity<JobSeeker>()
                .HasIndex(js => js.Phone)
                .IsUnique()
                .HasFilter("[Phone] IS NOT NULL");

            // Unique PhoneNumber on ApplicationUser where PhoneNumber is not null
            builder.Entity<ApplicationUser>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique()
                .HasFilter("[PhoneNumber] IS NOT NULL");
        }
    }
}