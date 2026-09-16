using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using WorkAt.Data;
using WorkAt.Models;

namespace WorkAt.Controllers
{
    [Authorize(Roles = "Company")]
    public class CompanyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CompanyController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Company  -> Company Dashboard
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var company = await _context.Companies
                .Include(c => c.User)
                .Include(c => c.Jobs)
                    .ThenInclude(j => j.Applications)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null) return NotFound();

            return View(company);
        }

        // GET: /Company/Edit -> shows the edit form
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = _userManager.GetUserId(User);
            var company = await _context.Companies
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null) return NotFound();
            return View(company);
        }

        // POST: /Company/Edit -> saves the edited profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Company model)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var company = await _context.Companies
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null || company.UserId != userId) return Forbid();

            ModelState.Remove(nameof(Company.UserId));

            // Validate phone uniqueness if changed
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
                    bool phoneInOtherCompany = await _context.Companies
                        .AnyAsync(c => c.CompanyId != company.CompanyId && c.Phone != null &&
                            c.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "") == cleanPhone);

                    bool phoneInJobSeeker = await _context.JobSeekers
                        .AnyAsync(js => js.Phone != null &&
                            js.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "") == cleanPhone);

                    if (phoneInOtherCompany || phoneInJobSeeker)
                    {
                        ModelState.AddModelError("Phone", "This phone number is already registered.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            company.CompanyName = model.CompanyName.Trim();
            company.Description = model.Description?.Trim();
            company.Address = model.Address?.Trim();
            company.Phone = model.Phone?.Trim();
            company.Website = model.Website?.Trim();

            // Update PhoneNumber on Identity user as well
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.PhoneNumber = company.Phone;
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

            TempData["SuccessMessage"] = "Company profile updated successfully.";
            return RedirectToAction("Index");
        }
    }
}