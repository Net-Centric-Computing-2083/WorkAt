using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkAt.Data;

using WorkAt.Models;

namespace WorkAt.Controllers
{
    public class JobsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JobsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? keyword,
            string? location,
            string? employmentType,
            int page = 1)
        {
            var jobs = _context.Jobs
                .Include(j => j.Company)
                .AsQueryable();

            // Multi-term partial keyword search
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
                        (j.Company != null && EF.Functions.Like(j.Company.CompanyName, $"%{tempTerm}%")));
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
                EmploymentType = employmentType
            };

            return View(viewModel);
        }
    }
}