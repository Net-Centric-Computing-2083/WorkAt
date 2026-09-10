using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkAt.Data;
using WorkAt.Models;

namespace WorkAt.Controllers
{
    [Authorize(Roles = "JobSeeker")]
    public class JobSeekerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public JobSeekerController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /JobSeeker
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var jobSeeker = await _context.JobSeekers
                .Include(js => js.Resume)
                .Include(js => js.Applications)
                    .ThenInclude(a => a.Job)
                        .ThenInclude(j => j!.Company)
                .FirstOrDefaultAsync(js => js.UserId == userId);

            if (jobSeeker == null)
            {
                return NotFound();
            }

            return View(jobSeeker);
        }

        // GET: /JobSeeker/EditProfile
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var jobSeeker = await _context.JobSeekers
                .FirstOrDefaultAsync(js => js.UserId == userId);

            if (jobSeeker == null)
            {
                return NotFound();
            }

            return View(jobSeeker);
        }

        // POST: /JobSeeker/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(JobSeeker model)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            // UserId is not submitted from the edit form,
            // so remove its validation error.
            ModelState.Remove(nameof(JobSeeker.UserId));

            // Find the actual JobSeeker belonging to the logged-in user
            var jobSeeker = await _context.JobSeekers
                .FirstOrDefaultAsync(js => js.UserId == userId);

            if (jobSeeker == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Update only the fields the user is allowed to edit
            jobSeeker.FirstName = model.FirstName;
            jobSeeker.LastName = model.LastName;
            jobSeeker.Phone = model.Phone;
            jobSeeker.Address = model.Address;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Your profile has been updated successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}