using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkAt.Data;
using WorkAt.Models;

namespace WorkAt.Controllers
{
    [Authorize(Roles = "Company")]
    public class JobController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public JobController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Job
        public async Task<IActionResult> Index(string? search, string? employmentType, string? sortOrder, int page = 1)
        {
            var userId = _userManager.GetUserId(User);

            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null)
            {
                return NotFound("Company profile not found.");
            }

            var query = _context.Jobs
                .Where(j => j.CompanyId == company.CompanyId)
                .AsQueryable();

            // Search filter: partial keywords across Title, Location, Description
            if (!string.IsNullOrWhiteSpace(search))
            {
                var terms = search.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in terms)
                {
                    var tempTerm = term;
                    query = query.Where(j =>
                        EF.Functions.Like(j.Title, $"%{tempTerm}%") ||
                        (j.Location != null && EF.Functions.Like(j.Location, $"%{tempTerm}%")) ||
                        EF.Functions.Like(j.Description, $"%{tempTerm}%"));
                }
            }

            // Employment Type filter
            if (!string.IsNullOrWhiteSpace(employmentType))
            {
                query = query.Where(j => j.EmploymentType == employmentType);
            }

            // Sorting
            sortOrder = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
            if (sortOrder == "asc")
            {
                query = query.OrderBy(j => j.PostedDate);
            }
            else
            {
                query = query.OrderByDescending(j => j.PostedDate);
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

            var viewModel = new CompanyJobsViewModel
            {
                Jobs = items,
                PageIndex = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = pageSize,
                Search = search,
                EmploymentType = employmentType,
                SortOrder = sortOrder
            };

            return View(viewModel);
        }

        // GET: Job/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null)
            {
                return NotFound("Company profile not found.");
            }

            var job = await _context.Jobs
                .FirstOrDefaultAsync(j =>
                    j.JobId == id &&
                    j.CompanyId == company.CompanyId);

            if (job == null)
            {
                return NotFound();
            }

            return View(job);
        }

        // GET: Job/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Job/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Job job)
        {
            var userId = _userManager.GetUserId(User);

            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null)
            {
                return NotFound("Company profile not found.");
            }

            if (!ModelState.IsValid)
            {
                return View(job);
            }

            // Never accept CompanyId from the form
            job.CompanyId = company.CompanyId;

            // Set PostedDate automatically
            job.PostedDate = DateTime.Now;

            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Job/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null)
            {
                return NotFound("Company profile not found.");
            }

            var job = await _context.Jobs
                .FirstOrDefaultAsync(j =>
                    j.JobId == id &&
                    j.CompanyId == company.CompanyId);

            if (job == null)
            {
                return NotFound();
            }

            return View(job);
        }

        // POST: Job/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Job job)
        {
            if (id != job.JobId)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null)
            {
                return NotFound("Company profile not found.");
            }

            // Ownership check
            var existingJob = await _context.Jobs
                .FirstOrDefaultAsync(j =>
                    j.JobId == id &&
                    j.CompanyId == company.CompanyId);

            if (existingJob == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(job);
            }

            // Update allowed fields only
            existingJob.Title = job.Title;
            existingJob.Description = job.Description;
            existingJob.Requirements = job.Requirements;
            existingJob.Location = job.Location;
            existingJob.Salary = job.Salary;
            existingJob.EmploymentType = job.EmploymentType;
            existingJob.Deadline = job.Deadline;

            // Do NOT change CompanyId or PostedDate

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Job/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null)
            {
                return NotFound("Company profile not found.");
            }

            var job = await _context.Jobs
                .FirstOrDefaultAsync(j =>
                    j.JobId == id &&
                    j.CompanyId == company.CompanyId);

            if (job == null)
            {
                return NotFound();
            }

            return View(job);
        }

        // POST: Job/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);

            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null)
            {
                return NotFound("Company profile not found.");
            }

            // Ownership check
            var job = await _context.Jobs
                .FirstOrDefaultAsync(j =>
                    j.JobId == id &&
                    j.CompanyId == company.CompanyId);

            if (job == null)
            {
                return NotFound();
            }

            _context.Jobs.Remove(job);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}