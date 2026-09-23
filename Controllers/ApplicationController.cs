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
        public async Task<IActionResult> Create(int jobId, IFormFile? resumeFile)
        {
            return await Apply(jobId, resumeFile);
        }

        [Authorize(Roles = "JobSeeker")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int jobId, IFormFile? resumeFile)
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

            // Verification Rule: Job Seeker must be verified by Admin
            if (jobSeeker.Status != "Verified")
            {
                TempData["ErrorMessage"] =
                    "Your account is awaiting Admin verification. You can apply for jobs after your account has been verified.";

                return RedirectToAction(
                    "Details",
                    "JobSearch",
                    new { id = jobId });
            }

            // Make sure the selected job exists
            var job = await _context.Jobs
                .FirstOrDefaultAsync(j => j.JobId == jobId);

            if (job == null)
            {
                return NotFound("Job not found.");
            }

            // Deadline check: Prevent applications after the job deadline has passed
            if (job.IsDeadlinePassed)
            {
                TempData["ErrorMessage"] =
                    "The application deadline for this position has passed. New applications are no longer being accepted.";

                return RedirectToAction(
                    "Details",
                    "JobSearch",
                    new { id = jobId });
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

            string? savedResumePath = null;
            string? originalFileName = null;

            // Handle PDF Resume Attachment if uploaded
            if (resumeFile != null && resumeFile.Length > 0)
            {
                // 1. Validate file extension (strictly .pdf only)
                var extension = Path.GetExtension(resumeFile.FileName).ToLowerInvariant();
                if (extension != ".pdf")
                {
                    TempData["ErrorMessage"] = "Only PDF files (.pdf) are accepted as resume attachments. Other file types (images, Word docs, videos) are not allowed.";
                    return RedirectToAction("Details", "JobSearch", new { id = jobId });
                }

                // 2. Validate MIME content type
                var contentType = resumeFile.ContentType?.ToLowerInvariant() ?? string.Empty;
                if (contentType != "application/pdf" && contentType != "application/x-pdf")
                {
                    TempData["ErrorMessage"] = "Only valid PDF documents are accepted. The uploaded file type is invalid.";
                    return RedirectToAction("Details", "JobSearch", new { id = jobId });
                }

                // 3. Validate file size (max 5 MB)
                if (resumeFile.Length > 5 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "The uploaded PDF resume exceeds the maximum allowed size of 5 MB.";
                    return RedirectToAction("Details", "JobSearch", new { id = jobId });
                }

                // 4. Validate PDF magic bytes (%PDF)
                using (var stream = resumeFile.OpenReadStream())
                {
                    byte[] header = new byte[4];
                    int bytesRead = await stream.ReadAsync(header, 0, 4);
                    if (bytesRead < 4 || header[0] != 0x25 || header[1] != 0x50 || header[2] != 0x44 || header[3] != 0x46) // "%PDF"
                    {
                        TempData["ErrorMessage"] = "Invalid PDF file. Please upload a genuine PDF document.";
                        return RedirectToAction("Details", "JobSearch", new { id = jobId });
                    }
                }

                // Safe unique filename
                var uniqueFileName = $"{Guid.NewGuid():N}.pdf";
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "resumes");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var physicalPath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(physicalPath, FileMode.Create))
                {
                    await resumeFile.CopyToAsync(fileStream);
                }

                savedResumePath = "/uploads/resumes/" + uniqueFileName;
                originalFileName = Path.GetFileName(resumeFile.FileName);
            }

            // Create the application
            var application = new Application
            {
                JobId = jobId,
                JobSeekerId = jobSeeker.JobSeekerId,
                AppliedDate = DateTime.UtcNow,
                Status = "Pending",
                ResumePath = savedResumePath,
                ResumeFileName = originalFileName
            };

            _context.Applications.Add(application);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Your application has been submitted successfully.";

            return RedirectToAction(nameof(MyApplications));
        }

        // =========================================================
        // RESUME DOWNLOAD / VIEW (PDF)
        // =========================================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DownloadResume(int applicationId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var application = await _context.Applications
                .Include(a => a.Job)
                .Include(a => a.JobSeeker)
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

            if (application == null || string.IsNullOrEmpty(application.ResumePath))
            {
                return NotFound("Resume not found.");
            }

            // Authorization check: User must be JobSeeker who applied, Company who owns job, or Admin
            bool isAuthorized = false;

            if (User.IsInRole("Admin"))
            {
                isAuthorized = true;
            }
            else if (User.IsInRole("JobSeeker"))
            {
                if (application.JobSeeker != null && application.JobSeeker.UserId == userId)
                {
                    isAuthorized = true;
                }
            }
            else if (User.IsInRole("Company"))
            {
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
                if (company != null && application.Job != null && application.Job.CompanyId == company.CompanyId)
                {
                    isAuthorized = true;
                }
            }

            if (!isAuthorized)
            {
                return Forbid();
            }

            var relativePath = application.ResumePath.TrimStart('/');
            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath.Replace('/', Path.DirectorySeparatorChar));

            if (!System.IO.File.Exists(physicalPath))
            {
                return NotFound("Resume file was not found on the server.");
            }

            var downloadName = !string.IsNullOrWhiteSpace(application.ResumeFileName)
                ? application.ResumeFileName
                : "Resume.pdf";

            return PhysicalFile(physicalPath, "application/pdf", downloadName);
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