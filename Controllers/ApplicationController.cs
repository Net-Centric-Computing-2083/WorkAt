using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkAt.Data;
using WorkAt.Models;

namespace WorkAt.Controllers
{
    [Authorize]
    public class ApplicationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ApplicationController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // JOBSEEKER: APPLY FOR A JOB
        // =========================================================

        [Authorize(Roles = "JobSeeker")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int jobId)
        {
            return await Apply(jobId);
        }

        [Authorize(Roles = "JobSeeker")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int jobId)
        {
            // Get the currently logged-in Identity user's ID
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // Find the JobSeeker profile belonging to the logged-in user
            var jobSeeker = await _context.JobSeekers
                .FirstOrDefaultAsync(js => js.UserId == userId);

            if (jobSeeker == null)
            {
                return NotFound("JobSeeker profile not found.");
            }

            // Make sure the selected job exists
            var job = await _context.Jobs
                .FirstOrDefaultAsync(j => j.JobId == jobId);

            if (job == null)
            {
                return NotFound("Job not found.");
            }

            // Prevent the same JobSeeker from applying to the same Job twice
            var alreadyApplied = await _context.Applications
                .AnyAsync(a =>
                    a.JobId == jobId &&
                    a.JobSeekerId == jobSeeker.JobSeekerId);

            if (alreadyApplied)
            {
                TempData["ErrorMessage"] =
                    "You have already applied for this job.";

                return RedirectToAction(
                    "Details",
                    "JobSearch",
                    new { id = jobId });
            }

            // Create the application
            var application = new Application
            {
                JobId = jobId,
                JobSeekerId = jobSeeker.JobSeekerId,
                AppliedDate = DateTime.UtcNow,
                Status = "Pending"
            };

            _context.Applications.Add(application);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Your application has been submitted successfully.";

            return RedirectToAction(nameof(MyApplications));
        }

        // =========================================================
        // JOBSEEKER: MY APPLICATIONS
        // =========================================================

        [Authorize(Roles = "JobSeeker")]
        [HttpGet]
        public async Task<IActionResult> MyApplications()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // Find the logged-in JobSeeker
            var jobSeeker = await _context.JobSeekers
                .FirstOrDefaultAsync(js => js.UserId == userId);

            if (jobSeeker == null)
            {
                return NotFound("JobSeeker profile not found.");
            }

            // Only retrieve applications belonging to this JobSeeker
            var applications = await _context.Applications
                .Include(a => a.Job)
                    .ThenInclude(j => j!.Company)
                .Where(a => a.JobSeekerId == jobSeeker.JobSeekerId)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

            return View(applications);
        }

        // =========================================================
        // COMPANY: VIEW APPLICATIONS FOR OWN JOBS
        // =========================================================

        [Authorize(Roles = "Company")]
        [HttpGet]
        public async Task<IActionResult> CompanyApplications()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // Find the Company belonging to the logged-in Identity user
            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null)
            {
                return NotFound("Company profile not found.");
            }

            // Only retrieve applications for jobs owned by this company
            var applications = await _context.Applications
                .Include(a => a.Job)
                .Include(a => a.JobSeeker)
                    .ThenInclude(js => js!.User)
                .Include(a => a.JobSeeker)
                    .ThenInclude(js => js!.Resume)
                .Where(a => a.Job != null &&
                            a.Job.CompanyId == company.CompanyId)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

            return View(applications);
        }

        // =========================================================
        // DETAILS: VIEW APPLICATION DETAILS (JOBSEEKER OR COMPANY)
        // =========================================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            if (User.IsInRole("JobSeeker"))
            {
                var jobSeeker = await _context.JobSeekers
                    .FirstOrDefaultAsync(js => js.UserId == userId);

                if (jobSeeker == null)
                {
                    return NotFound("JobSeeker profile not found.");
                }

                // Strict ownership check: Application must belong to this JobSeeker
                var application = await _context.Applications
                    .Include(a => a.Job)
                        .ThenInclude(j => j!.Company)
                    .FirstOrDefaultAsync(a => a.ApplicationId == id &&
                                              a.JobSeekerId == jobSeeker.JobSeekerId);

                if (application == null)
                {
                    return NotFound();
                }

                return View(application);
            }
            else if (User.IsInRole("Company"))
            {
                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (company == null)
                {
                    return NotFound("Company profile not found.");
                }

                // Strict ownership check: Application's Job must belong to this Company
                var application = await _context.Applications
                    .Include(a => a.Job)
                    .Include(a => a.JobSeeker)
                        .ThenInclude(js => js!.User)
                    .Include(a => a.JobSeeker)
                        .ThenInclude(js => js!.Resume)
                            .ThenInclude(r => r!.ResumeSkills)
                                .ThenInclude(rs => rs.Skill)
                    .FirstOrDefaultAsync(a => a.ApplicationId == id &&
                                              a.Job != null &&
                                              a.Job.CompanyId == company.CompanyId);

                if (application == null)
                {
                    return NotFound();
                }

                return View(application);
            }

            return Forbid();
        }

        // =========================================================
        // COMPANY: ACCEPT APPLICATION
        // =========================================================

        [Authorize(Roles = "Company")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            var application = await GetCompanyOwnedApplication(id);

            if (application == null)
                return NotFound();

            if (application.Status != "Pending")
            {
                TempData["ErrorMessage"] = "This application has already been processed.";
                return RedirectToAction(nameof(CompanyApplications));
            }

            application.Status = "Accepted";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Application has been accepted.";
            return RedirectToAction(nameof(CompanyApplications));
        }

        // =========================================================
        // COMPANY: REJECT APPLICATION
        // =========================================================

        [Authorize(Roles = "Company")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var application = await GetCompanyOwnedApplication(id);

            if (application == null)
                return NotFound();

            if (application.Status != "Pending")
            {
                TempData["ErrorMessage"] = "This application has already been processed.";
                return RedirectToAction(nameof(CompanyApplications));
            }

            application.Status = "Rejected";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Application has been rejected.";
            return RedirectToAction(nameof(CompanyApplications));
        }

        // =========================================================
        // HELPER: VERIFY COMPANY OWNERSHIP
        // =========================================================

        private async Task<Application?> GetCompanyOwnedApplication(int applicationId)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            // Find the logged-in company
            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null)
            {
                return null;
            }

            // Get the application together with its Job
            var application = await _context.Applications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

            if (application == null || application.Job == null)
            {
                return null;
            }

            // Ownership check:
            // The application's Job must belong to the logged-in company.
            if (application.Job.CompanyId != company.CompanyId)
            {
                return null;
            }

            return application;
        }
    }
}