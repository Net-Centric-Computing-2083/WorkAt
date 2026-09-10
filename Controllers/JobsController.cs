using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkAt.Data;

namespace WorkAt.Controllers
{
    public class JobsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JobsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Jobs
        public async Task<IActionResult> Index()
        {
            var jobs = await _context.Jobs
                .OrderByDescending(j => j.PostedDate)
                .ToListAsync();

            return View(jobs);
        }
    }
}