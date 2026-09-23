using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkAt.Data;
using WorkAt.Models;

namespace WorkAt.Controllers
{
    public class JobSearchController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public JobSearchController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /JobSearch
        public async Task<IActionResult> Index(
            string? keyword,
            string? location,
            string? employmentType,
            string? salary,
            int page = 1)
        {
            var now = DateTime.UtcNow;
            var today = DateTime.UtcNow.Date;

            // Only show active jobs whose application deadline has not passed
            var jobs = _context.Jobs
                .Include(j => j.Company)
                .Where(j => j.Deadline == null || j.Deadline.Value >= now || j.Deadline.Value.Date >= today)
                .AsQueryable();

            // Multi-term partial keyword search across title, description, requirements, company name, location
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var terms = keyword.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in terms)
                {
                    var tempTerm = term;
                    jobs = jobs.Where(j =>
                        EF.Functions.Like(j.Title, $"%{tempTerm}%") ||
                        EF.Functions.Like(j.Description, $"%{tempTerm}%") ||
                        (j.Requirements != null && EF.Functions.Like(j.Requirements, $"%{tempTerm}%")) ||
                        (j.Company != null && EF.Functions.Like(j.Company.CompanyName, $"%{tempTerm}%")) ||
                        (j.Location != null && EF.Functions.Like(j.Location, $"%{tempTerm}%")));
                }
            }

            // Location filter
            if (!string.IsNullOrWhiteSpace(location))
            {
                var locTerms = location.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in locTerms)
                {
                    var tempTerm = term;
                    jobs = jobs.Where(j => j.Location != null && EF.Functions.Like(j.Location, $"%{tempTerm}%"));
                }
            }

            // Employment type filter
            if (!string.IsNullOrWhiteSpace(employmentType))
            {
                jobs = jobs.Where(j => j.EmploymentType == employmentType);
            }

            // Salary filter
            if (!string.IsNullOrWhiteSpace(salary))
            {
                var salTrim = salary.Trim();
                if (salTrim == "under-30k")
                {
                    jobs = jobs.Where(j => j.Salary != null && (
                        EF.Functions.Like(j.Salary, "%10,000%") ||
                        EF.Functions.Like(j.Salary, "%15,000%") ||
                        EF.Functions.Like(j.Salary, "%20,000%") ||
                        EF.Functions.Like(j.Salary, "%25,000%") ||
                        EF.Functions.Like(j.Salary, "%30,000%") ||
                        EF.Functions.Like(j.Salary, "%10000%") ||
                        EF.Functions.Like(j.Salary, "%15000%") ||
                        EF.Functions.Like(j.Salary, "%20000%") ||
                        EF.Functions.Like(j.Salary, "%25000%") ||
                        EF.Functions.Like(j.Salary, "%30000%")));
                }
                else if (salTrim == "30k-50k")
                {
                    jobs = jobs.Where(j => j.Salary != null && (
                        EF.Functions.Like(j.Salary, "%30,000%") ||
                        EF.Functions.Like(j.Salary, "%35,000%") ||
                        EF.Functions.Like(j.Salary, "%40,000%") ||
                        EF.Functions.Like(j.Salary, "%45,000%") ||
                        EF.Functions.Like(j.Salary, "%50,000%") ||
                        EF.Functions.Like(j.Salary, "%30000%") ||
                        EF.Functions.Like(j.Salary, "%35000%") ||
                        EF.Functions.Like(j.Salary, "%40000%") ||
                        EF.Functions.Like(j.Salary, "%45000%") ||
                        EF.Functions.Like(j.Salary, "%50000%")));
                }
                else if (salTrim == "50k-100k" || salTrim == "50k+")
                {
                    jobs = jobs.Where(j => j.Salary != null && (
                        EF.Functions.Like(j.Salary, "%50,000%") ||
                        EF.Functions.Like(j.Salary, "%60,000%") ||
                        EF.Functions.Like(j.Salary, "%70,000%") ||
                        EF.Functions.Like(j.Salary, "%80,000%") ||
                        EF.Functions.Like(j.Salary, "%90,000%") ||
                        EF.Functions.Like(j.Salary, "%100,000%") ||
                        EF.Functions.Like(j.Salary, "%50000%") ||
                        EF.Functions.Like(j.Salary, "%60000%") ||
                        EF.Functions.Like(j.Salary, "%70000%") ||
                        EF.Functions.Like(j.Salary, "%80000%") ||
                        EF.Functions.Like(j.Salary, "%90000%") ||
                        EF.Functions.Like(j.Salary, "%100000%") ||
                        EF.Functions.Like(j.Salary, "%50,000+%") ||
                        EF.Functions.Like(j.Salary, "%50000+%")));
                }
                else if (salTrim == "100k+")
                {
                    jobs = jobs.Where(j => j.Salary != null && (
                        EF.Functions.Like(j.Salary, "%100,000%") ||
                        EF.Functions.Like(j.Salary, "%120,000%") ||
                        EF.Functions.Like(j.Salary, "%150,000%") ||
                        EF.Functions.Like(j.Salary, "%200,000%") ||
                        EF.Functions.Like(j.Salary, "%100000%") ||
                        EF.Functions.Like(j.Salary, "%100k%") ||
                        EF.Functions.Like(j.Salary, "%100,000+%")));
                }
                else
                {
                    jobs = jobs.Where(j => j.Salary != null && EF.Functions.Like(j.Salary, $"%{salTrim}%"));
                }
            }

            int pageSize = 9;
            int totalItems = await jobs.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var result = await jobs
                .OrderByDescending(j => j.PostedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new BrowseJobsViewModel
            {
                Jobs = result,
                PageIndex = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = pageSize,
                Keyword = keyword,
                Location = location,
                EmploymentType = employmentType,
                Salary = salary
            };

            return View(viewModel);
        }

        // GET: /JobSearch/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var job = await _context.Jobs
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.JobId == id);

            if (job == null)
            {
                return NotFound();
            }

            ViewBag.IsExpired = job.IsDeadlinePassed;

            if (User.Identity?.IsAuthenticated == true && User.IsInRole("JobSeeker"))
            {
                var userId = _userManager.GetUserId(User);
                if (!string.IsNullOrEmpty(userId))
                {
                    var jobSeeker = await _context.JobSeekers
                        .FirstOrDefaultAsync(js => js.UserId == userId);

                    if (jobSeeker != null)
                    {
                        ViewBag.JobSeekerStatus = jobSeeker.Status;

                        var application = await _context.Applications
                            .FirstOrDefaultAsync(a => a.JobId == id && a.JobSeekerId == jobSeeker.JobSeekerId);

                        if (application != null)
                        {
                            ViewBag.HasApplied = true;
                            ViewBag.ApplicationStatus = application.Status;
                        }
                    }
                }
            }

            return View(job);
        }
    }
}