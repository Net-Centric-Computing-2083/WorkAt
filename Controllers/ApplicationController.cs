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
        public async Task<IActionResult> MyApplications(string? search, string? status, string? sortOrder, int page = 1)
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

            var query = _context.Applications
                .Include(a => a.Job)
                    .ThenInclude(j => j!.Company)
                .Where(a => a.JobSeekerId == jobSeeker.JobSeekerId)
                .AsQueryable();

            // Search filter: job title or company name
            if (!string.IsNullOrWhiteSpace(search))
            {
                var terms = search.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in terms)
                {
                    var tempTerm = term;
                    query = query.Where(a =>
                        (a.Job != null && a.Job.Title != null && EF.Functions.Like(a.Job.Title, $"%{tempTerm}%")) ||
                        (a.Job != null && a.Job.Company != null && a.Job.Company.CompanyName != null && EF.Functions.Like(a.Job.Company.CompanyName, $"%{tempTerm}%")));
                }
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(a => a.Status == status);
            }

            // Sorting
            sortOrder = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
            if (sortOrder == "asc")
            {
                query = query.OrderBy(a => a.AppliedDate);
            }
            else
            {
                query = query.OrderByDescending(a => a.AppliedDate);
            }

            // Pagination
            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new MyApplicationsViewModel
            {
                Applications = items,
                PageIndex = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = pageSize,
                Search = search,
                Status = status,
                SortOrder = sortOrder
            };

            return View(viewModel);
        }

        // =========================================================
        // COMPANY: VIEW APPLICATIONS FOR OWN JOBS
        // =========================================================

        [Authorize(Roles = "Company")]
        [HttpGet]
        public async Task<IActionResult> CompanyApplications(string? search, string? status, string? sortOrder, int page = 1)
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

            var query = _context.Applications
                .Include(a => a.Job)
                .Include(a => a.JobSeeker)
                    .ThenInclude(js => js!.User)
                .Include(a => a.JobSeeker)
                    .ThenInclude(js => js!.Resume)
                .Include(a => a.Feedback)
                .Where(a => a.Job != null && a.Job.CompanyId == company.CompanyId)
                .AsQueryable();

            // Search filter: applicant name or job title
            if (!string.IsNullOrWhiteSpace(search))
            {
                var terms = search.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in terms)
                {
                    var tempTerm = term;
                    query = query.Where(a =>
                        (a.JobSeeker != null && (
                            EF.Functions.Like(a.JobSeeker.FirstName, $"%{tempTerm}%") ||
                            EF.Functions.Like(a.JobSeeker.LastName, $"%{tempTerm}%") ||
                            EF.Functions.Like(a.JobSeeker.FirstName + " " + a.JobSeeker.LastName, $"%{tempTerm}%")
                        )) ||
                        (a.Job != null && EF.Functions.Like(a.Job.Title, $"%{tempTerm}%")));
                }
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(a => a.Status == status);
            }

            // Sorting
            sortOrder = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
            if (sortOrder == "asc")
            {
                query = query.OrderBy(a => a.AppliedDate);
            }
            else
            {
                query = query.OrderByDescending(a => a.AppliedDate);
            }

            // Pagination
            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new CompanyApplicationsViewModel
            {
                Applications = items,
                PageIndex = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = pageSize,
                Search = search,
                Status = status,
                SortOrder = sortOrder
            };

            return View(viewModel);
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
                    .Include(a => a.JobSeeker)
                        .ThenInclude(js => js!.User)
                    .Include(a => a.JobSeeker)
                        .ThenInclude(js => js!.Resume)
                            .ThenInclude(r => r!.ResumeSkills)
                                .ThenInclude(rs => rs.Skill)
                    .Include(a => a.Feedback)
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
                        .ThenInclude(j => j!.Company)
                    .Include(a => a.JobSeeker)
                        .ThenInclude(js => js!.User)
                    .Include(a => a.JobSeeker)
                        .ThenInclude(js => js!.Resume)
                            .ThenInclude(r => r!.ResumeSkills)
                                .ThenInclude(rs => rs.Skill)
                    .Include(a => a.Feedback)
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
        public async Task<IActionResult> Accept(int id, string? feedbackText)
        {
            var application = await GetCompanyOwnedApplication(id);

            if (application == null)
                return NotFound();

            application.Status = "Accepted";

            await SaveOrUpdateFeedbackAsync(id, feedbackText);

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
        public async Task<IActionResult> Reject(int id, string? feedbackText)
        {
            var application = await GetCompanyOwnedApplication(id);

            if (application == null)
                return NotFound();

            // Rejection requires feedback text
            if (string.IsNullOrWhiteSpace(feedbackText))
            {
                TempData["ErrorMessage"] = "Feedback is required when rejecting an application.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            application.Status = "Rejected";

            await SaveOrUpdateFeedbackAsync(id, feedbackText);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Application has been rejected.";
            return RedirectToAction(nameof(CompanyApplications));
        }

        // =========================================================
        // HELPER: SAVE OR UPDATE FEEDBACK
        // =========================================================

        private async Task SaveOrUpdateFeedbackAsync(int applicationId, string? feedbackText)
        {
            if (string.IsNullOrWhiteSpace(feedbackText))
            {
                return;
            }

            var existingFeedback = await _context.ApplicationFeedbacks
                .FirstOrDefaultAsync(f => f.ApplicationId == applicationId);

            if (existingFeedback != null)
            {
                existingFeedback.FeedbackText = feedbackText;
                existingFeedback.FeedbackDate = DateTime.UtcNow;
            }
            else
            {
                var feedback = new ApplicationFeedback
                {
                    ApplicationId = applicationId,
                    FeedbackText = feedbackText,
                    FeedbackDate = DateTime.UtcNow
                };

                _context.ApplicationFeedbacks.Add(feedback);
            }
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