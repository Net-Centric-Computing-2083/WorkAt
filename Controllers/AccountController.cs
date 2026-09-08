using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WorkAt.Data;
using WorkAt.Models;

namespace WorkAt.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (model.AccountType != "Company" &&
                model.AccountType != "JobSeeker")
            {
                ModelState.AddModelError(
                    "AccountType",
                    "Please select a valid account type.");
            }

            if (model.AccountType == "Company" &&
                string.IsNullOrWhiteSpace(model.CompanyName))
            {
                ModelState.AddModelError(
                    "CompanyName",
                    "Company name is required.");
            }

            if (model.AccountType == "JobSeeker")
            {
                if (string.IsNullOrWhiteSpace(model.FirstName))
                {
                    ModelState.AddModelError(
                        "FirstName",
                        "First name is required.");
                }

                if (string.IsNullOrWhiteSpace(model.LastName))
                {
                    ModelState.AddModelError(
                        "LastName",
                        "Last name is required.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(
                user,
                model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(model);
            }

            var roleResult = await _userManager.AddToRoleAsync(
                user,
                model.AccountType);

            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                await _userManager.DeleteAsync(user);

                return View(model);
            }

            if (model.AccountType == "Company")
            {
                var company = new Company
                {
                    CompanyName = model.CompanyName!,
                    UserId = user.Id
                };

                _context.Companies.Add(company);
            }
            else
            {
                var jobSeeker = new JobSeeker
                {
                    FirstName = model.FirstName!,
                    LastName = model.LastName!,
                    UserId = user.Id
                };

                _context.JobSeekers.Add(jobSeeker);
            }

            await _context.SaveChangesAsync();

            await _signInManager.SignInAsync(
                user,
                isPersistent: false);

            return RedirectToAction("Index", "Home");
        }
    }
}