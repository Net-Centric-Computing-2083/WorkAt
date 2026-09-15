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
            string? employmentType)
        {
            var jobs = _context.Jobs
                .Include(j => j.Company)
                .AsQueryable();

            // Keyword search
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                jobs = jobs.Where(j =>
                    j.Title.Contains(keyword) ||
                    j.Description.Contains(keyword));
            }

            // Location filter
            if (!string.IsNullOrWhiteSpace(location))
            {
                jobs = jobs.Where(j =>
                    j.Location != null &&
                    j.Location.Contains(location));
            }

            // Employment type filter
            if (!string.IsNullOrWhiteSpace(employmentType))
            {
                jobs = jobs.Where(j =>
                    j.EmploymentType == employmentType);
            }

            var result = await jobs
                .OrderByDescending(j => j.PostedDate)
                .ToListAsync();

            ViewBag.Keyword = keyword;
            ViewBag.Location = location;
            ViewBag.EmploymentType = employmentType;

            return View(result);
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

            if (User.Identity?.IsAuthenticated == true && User.IsInRole("JobSeeker"))
            {
                var userId = _userManager.GetUserId(User);
                if (!string.IsNullOrEmpty(userId))
                {
                    var application = await _context.Applications
                        .FirstOrDefaultAsync(a => a.JobId == id && a.JobSeeker != null && a.JobSeeker.UserId == userId);

                    if (application != null)
                    {
                        ViewBag.HasApplied = true;
                        ViewBag.ApplicationStatus = application.Status;
                    }
                }
            }

            return View(job);
        }
    }
}