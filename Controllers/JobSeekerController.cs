using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
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
                .Include(js => js.User)
                .Include(js => js.Resume)
                    .ThenInclude(r => r!.ResumeSkills)
                        .ThenInclude(rs => rs.Skill)
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
                .Include(js => js.User)
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

            // UserId is not submitted from the edit form
            ModelState.Remove(nameof(JobSeeker.UserId));

            var jobSeeker = await _context.JobSeekers
                .Include(js => js.User)
                .FirstOrDefaultAsync(js => js.UserId == userId);

            if (jobSeeker == null)
            {
                return NotFound();
            }

            // Validate phone format & uniqueness if changed
            if (!string.IsNullOrWhiteSpace(model.Phone))
            {
                string digitsOnly = Regex.Replace(model.Phone, @"\D", "");
                if (digitsOnly.Length < 7 || digitsOnly.Length > 15)
                {
                    ModelState.AddModelError("Phone", "Please enter a valid phone number with 7 to 15 digits.");
                }
                else
                {
                    string cleanPhone = Regex.Replace(model.Phone, @"[\s\-\(\)\+]", "");
                    bool phoneInOtherJobSeeker = await _context.JobSeekers
                        .AnyAsync(js => js.JobSeekerId != jobSeeker.JobSeekerId && js.Phone != null &&
                            js.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "") == cleanPhone);

                    bool phoneInCompany = await _context.Companies
                        .AnyAsync(c => c.Phone != null &&
                            c.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "") == cleanPhone);

                    if (phoneInOtherJobSeeker || phoneInCompany)
                    {
                        ModelState.AddModelError("Phone", "This phone number is already registered.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Update allowed fields
            jobSeeker.FirstName = model.FirstName.Trim();
            jobSeeker.LastName = model.LastName.Trim();
            jobSeeker.Phone = model.Phone?.Trim();
            jobSeeker.Address = model.Address?.Trim();

            // Synchronize Identity user PhoneNumber
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.PhoneNumber = jobSeeker.Phone;
                await _userManager.UpdateAsync(user);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "This phone number is already in use by another account.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Your profile has been updated successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}