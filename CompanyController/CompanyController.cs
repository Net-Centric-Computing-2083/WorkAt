using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
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
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null) return NotFound();

            return View(company);
        }

        // GET: /Company/Edit -> shows the edit form
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = _userManager.GetUserId(User);
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
            if (company == null) return NotFound();
            return View(company);
        }

        // POST: /Company/Edit -> saves the edited profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Company model)
        {
            var userId = _userManager.GetUserId(User);
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null || company.UserId != userId) return Forbid();

            company.CompanyName = model.CompanyName;
            company.Description = model.Description;
            company.Address = model.Address;
            company.Phone = model.Phone;
            company.Website = model.Website;

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}