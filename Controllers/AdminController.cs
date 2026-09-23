using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkAt.Data;
using WorkAt.Models;

namespace WorkAt.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AdminController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // =========================================================
        // ADMIN AUTHENTICATION & REGISTRATION
        // =========================================================

        [AllowAnonymous]
        [HttpGet("Admin/Registration")]
        public IActionResult Registration()
        {
            if (_signInManager.IsSignedIn(User) && User.IsInRole("Admin"))
            {
                return RedirectToAction(nameof(Index));
            }

            return View(new AdminRegisterViewModel());
        }

        [AllowAnonymous]
        [HttpPost("Admin/Registration")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registration(AdminRegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email?.Trim() ?? string.Empty;

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email address already exists.");
                return View(model);
            }

            var adminUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(adminUser, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    if (error.Code == "DuplicateEmail" || error.Code == "DuplicateUserName")
                    {
                        ModelState.AddModelError("Email", "An account with this email address already exists.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                return View(model);
            }

            await _userManager.AddToRoleAsync(adminUser, "Admin");

            var adminProfile = new Admin
            {
                FullName = email.Split('@')[0],
                UserId = adminUser.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Admins.Add(adminProfile);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Admin account registered successfully! Please sign in with your credentials.";
            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        [HttpGet("Admin/Login")]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User))
            {
                if (User.IsInRole("Admin"))
                {
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    await _signInManager.SignOutAsync();
                }
            }

            ViewData["ReturnUrl"] = returnUrl ?? Url.Action(nameof(Index), "Admin");
            return View(new LoginViewModel());
        }

        [AllowAnonymous]
        [HttpPost("Admin/Login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email?.Trim() ?? string.Empty;
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid admin credentials.");
                return View(model);
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (!isAdmin)
            {
                ModelState.AddModelError(string.Empty, "Access denied. This account does not possess administrator privileges.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction(nameof(Index));
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Admin account temporarily locked out.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid admin credentials.");
            return View(model);
        }

        [HttpPost("Admin/Logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        // =========================================================
        // DASHBOARD
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var now = DateTime.UtcNow;
            var today = DateTime.UtcNow.Date;

            // 1. Company statistics
            var totalCompanies = await _context.Companies.CountAsync();
            var pendingCompanies = await _context.Companies.CountAsync(c => c.Status == "Pending");
            var acceptedCompanies = await _context.Companies.CountAsync(c => c.Status == "Verified" || c.Status == "Accepted");
            var rejectedCompanies = await _context.Companies.CountAsync(c => c.Status == "Rejected");

            // 2. Job Seeker statistics
            var totalJobSeekers = await _context.JobSeekers.CountAsync();
            var pendingJobSeekers = await _context.JobSeekers.CountAsync(js => js.Status == "Pending");
            var acceptedJobSeekers = await _context.JobSeekers.CountAsync(js => js.Status == "Verified" || js.Status == "Accepted");
            var rejectedJobSeekers = await _context.JobSeekers.CountAsync(js => js.Status == "Rejected");

            // 3. Job statistics
            var totalJobs = await _context.Jobs.CountAsync();
            var activeJobs = await _context.Jobs.CountAsync(j => j.Deadline == null || j.Deadline.Value >= now || j.Deadline.Value.Date >= today);
            var expiredJobs = totalJobs - activeJobs;

            // 4. Application statistics
            var totalApplications = await _context.Applications.CountAsync();
            var pendingApplications = await _context.Applications.CountAsync(a => a.Status == "Pending" || a.Status == "Submitted" || a.Status == "UnderReview");
            var acceptedApplications = await _context.Applications.CountAsync(a => a.Status == "Accepted" || a.Status == "Shortlisted");
            var rejectedApplications = await _context.Applications.CountAsync(a => a.Status == "Rejected");

            // 5. Queues and Recent Records
            var recentPendingCompanies = await _context.Companies
                .Include(c => c.User)
                .Where(c => c.Status == "Pending")
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentPendingJobSeekers = await _context.JobSeekers
                .Include(js => js.User)
                .Where(js => js.Status == "Pending")
                .OrderByDescending(js => js.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentApplications = await _context.Applications
                .Include(a => a.Job)
                    .ThenInclude(j => j!.Company)
                .Include(a => a.JobSeeker)
                .OrderByDescending(a => a.AppliedDate)
                .Take(5)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                TotalCompanies = totalCompanies,
                PendingCompanies = pendingCompanies,
                AcceptedCompanies = acceptedCompanies,
                RejectedCompanies = rejectedCompanies,

                TotalJobSeekers = totalJobSeekers,
                PendingJobSeekers = pendingJobSeekers,
                AcceptedJobSeekers = acceptedJobSeekers,
                RejectedJobSeekers = rejectedJobSeekers,

                TotalJobs = totalJobs,
                ActiveJobs = activeJobs,
                ExpiredJobs = expiredJobs,

                TotalApplications = totalApplications,
                PendingApplications = pendingApplications,
                AcceptedApplications = acceptedApplications,
                RejectedApplications = rejectedApplications,

                RecentPendingCompanies = recentPendingCompanies,
                RecentPendingJobSeekers = recentPendingJobSeekers,
                RecentApplications = recentApplications
            };

            return View(model);
        }

        // =========================================================
        // MANAGE COMPANIES
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Companies(string? search, string? status, int page = 1)
        {
            var query = _context.Companies
                .Include(c => c.User)
                .Include(c => c.Jobs)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(c =>
                    EF.Functions.Like(c.CompanyName, $"%{term}%") ||
                    (c.User != null && c.User.Email != null && EF.Functions.Like(c.User.Email, $"%{term}%")) ||
                    (c.Phone != null && EF.Functions.Like(c.Phone, $"%{term}%")) ||
                    (c.Address != null && EF.Functions.Like(c.Address, $"%{term}%")));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c => c.Status == status);
            }

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new AdminCompaniesViewModel
            {
                Companies = items,
                PageIndex = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = pageSize,
                Search = search,
                Status = status
            };

            return View(viewModel);
        }

        // =========================================================
        // MANAGE JOB SEEKERS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> JobSeekers(string? search, string? status, int page = 1)
        {
            var query = _context.JobSeekers
                .Include(js => js.User)
                .Include(js => js.Applications)
                .Include(js => js.Resume)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(js =>
                    EF.Functions.Like(js.FirstName, $"%{term}%") ||
                    EF.Functions.Like(js.LastName, $"%{term}%") ||
                    EF.Functions.Like(js.FirstName + " " + js.LastName, $"%{term}%") ||
                    (js.User != null && js.User.Email != null && EF.Functions.Like(js.User.Email, $"%{term}%")) ||
                    (js.Phone != null && EF.Functions.Like(js.Phone, $"%{term}%")));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(js => js.Status == status);
            }

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(js => js.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new AdminJobSeekersViewModel
            {
                JobSeekers = items,
                PageIndex = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = pageSize,
                Search = search,
                Status = status
            };

            return View(viewModel);
        }

        // =========================================================
        // DETAILS: COMPANY
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> CompanyDetails(int id)
        {
            var company = await _context.Companies
                .Include(c => c.User)
                .Include(c => c.Jobs)
                    .ThenInclude(j => j.Applications)
                .FirstOrDefaultAsync(c => c.CompanyId == id);

            if (company == null)
            {
                return NotFound();
            }

            return View(company);
        }

        // =========================================================
        // DETAILS: JOB SEEKER
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> JobSeekerDetails(int id)
        {
            var jobSeeker = await _context.JobSeekers
                .Include(js => js.User)
                .Include(js => js.Resume)
                    .ThenInclude(r => r!.ResumeSkills)
                        .ThenInclude(rs => rs.Skill)
                .Include(js => js.Applications)
                    .ThenInclude(a => a.Job)
                        .ThenInclude(j => j!.Company)
                .FirstOrDefaultAsync(js => js.JobSeekerId == id);

            if (jobSeeker == null)
            {
                return NotFound();
            }

            return View(jobSeeker);
        }

        // =========================================================
        // VERIFICATION ACTIONS: COMPANY
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyCompany(int id, string? returnUrl = null)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null)
            {
                return NotFound();
            }

            company.Status = "Verified";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Company '{company.CompanyName}' has been verified successfully.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Companies));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectCompany(int id, string? returnUrl = null)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null)
            {
                return NotFound();
            }

            company.Status = "Rejected";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Company '{company.CompanyName}' status has been set to Rejected.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Companies));
        }

        // =========================================================
        // VERIFICATION ACTIONS: JOB SEEKER
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyJobSeeker(int id, string? returnUrl = null)
        {
            var jobSeeker = await _context.JobSeekers.FindAsync(id);
            if (jobSeeker == null)
            {
                return NotFound();
            }

            jobSeeker.Status = "Verified";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Job Seeker '{jobSeeker.FirstName} {jobSeeker.LastName}' has been verified successfully.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(JobSeekers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectJobSeeker(int id, string? returnUrl = null)
        {
            var jobSeeker = await _context.JobSeekers.FindAsync(id);
            if (jobSeeker == null)
            {
                return NotFound();
            }

            jobSeeker.Status = "Rejected";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Job Seeker '{jobSeeker.FirstName} {jobSeeker.LastName}' status has been set to Rejected.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(JobSeekers));
        }

        // =========================================================
        // ALL APPLICATIONS LOG
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Applications(string? search, string? status, int page = 1)
        {
            var query = _context.Applications
                .Include(a => a.Job)
                    .ThenInclude(j => j!.Company)
                .Include(a => a.JobSeeker)
                    .ThenInclude(js => js!.User)
                .Include(a => a.Feedback)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(a =>
                    (a.Job != null && a.Job.Title != null && EF.Functions.Like(a.Job.Title, $"%{term}%")) ||
                    (a.Job != null && a.Job.Company != null && a.Job.Company.CompanyName != null && EF.Functions.Like(a.Job.Company.CompanyName, $"%{term}%")) ||
                    (a.JobSeeker != null && (
                        EF.Functions.Like(a.JobSeeker.FirstName, $"%{term}%") ||
                        EF.Functions.Like(a.JobSeeker.LastName, $"%{term}%") ||
                        EF.Functions.Like(a.JobSeeker.FirstName + " " + a.JobSeeker.LastName, $"%{term}%")
                    )));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(a => a.Status == status);
            }

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(a => a.AppliedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new AdminApplicationsViewModel
            {
                Applications = items,
                PageIndex = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = pageSize,
                Search = search,
                Status = status
            };

            return View(viewModel);
        }

        // =========================================================
        // DETAILS: APPLICATION
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> ApplicationDetails(int id)
        {
            var application = await _context.Applications
                .Include(a => a.Job)
                    .ThenInclude(j => j!.Company)
                        .ThenInclude(c => c!.User)
                .Include(a => a.JobSeeker)
                    .ThenInclude(js => js!.User)
                .Include(a => a.JobSeeker)
                    .ThenInclude(js => js!.Resume)
                        .ThenInclude(r => r!.ResumeSkills)
                            .ThenInclude(rs => rs.Skill)
                .Include(a => a.Feedback)
                .FirstOrDefaultAsync(a => a.ApplicationId == id);

            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }
    }
}
