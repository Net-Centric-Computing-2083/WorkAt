using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
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

        // =========================================================
        // REGISTRATION
        // =========================================================

        [HttpGet]
        public IActionResult Register(string? role = null)
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectUserByRole();
            }

            var model = new RegisterViewModel();
            if (role == "Company" || role == "JobSeeker")
            {
                model.AccountType = role;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            // Normalize and trim inputs
            model.Email = model.Email?.Trim() ?? string.Empty;
            model.Phone = model.Phone?.Trim() ?? string.Empty;
            model.FirstName = model.FirstName?.Trim();
            model.LastName = model.LastName?.Trim();
            model.CompanyName = model.CompanyName?.Trim();
            model.Address = model.Address?.Trim();
            model.Website = model.Website?.Trim();
            model.Description = model.Description?.Trim();

            // Validate Account Type
            if (model.AccountType != "Company" && model.AccountType != "JobSeeker")
            {
                ModelState.AddModelError("AccountType", "Please select a valid account type (Job Seeker or Company).");
            }

            // Role-specific required fields
            if (model.AccountType == "Company")
            {
                if (string.IsNullOrWhiteSpace(model.CompanyName))
                {
                    ModelState.AddModelError("CompanyName", "Company name is required.");
                }
            }
            else if (model.AccountType == "JobSeeker")
            {
                if (string.IsNullOrWhiteSpace(model.FirstName))
                {
                    ModelState.AddModelError("FirstName", "First name is required.");
                }

                if (string.IsNullOrWhiteSpace(model.LastName))
                {
                    ModelState.AddModelError("LastName", "Last name is required.");
                }
            }

            // Validate phone number format & reasonable length
            if (string.IsNullOrWhiteSpace(model.Phone))
            {
                ModelState.AddModelError("Phone", "Phone number is required.");
            }
            else
            {
                string digitsOnly = Regex.Replace(model.Phone, @"\D", "");
                if (digitsOnly.Length < 7 || digitsOnly.Length > 15)
                {
                    ModelState.AddModelError("Phone", "Please enter a valid phone number with 7 to 15 digits.");
                }
                else if (new string(digitsOnly[0], digitsOnly.Length) == digitsOnly)
                {
                    ModelState.AddModelError("Phone", "Please enter a valid phone number.");
                }
            }

            // Global Email Uniqueness Check (cross-table / cross-role)
            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                var existingUserByEmail = await _userManager.FindByEmailAsync(model.Email);
                if (existingUserByEmail != null)
                {
                    ModelState.AddModelError("Email", "An account with this email address already exists.");
                }
                else
                {
                    bool emailInCompany = await _context.Companies
                        .AnyAsync(c => c.User != null && c.User.NormalizedEmail == model.Email.ToUpper());
                    bool emailInJobSeeker = await _context.JobSeekers
                        .AnyAsync(js => js.User != null && js.User.NormalizedEmail == model.Email.ToUpper());

                    if (emailInCompany || emailInJobSeeker)
                    {
                        ModelState.AddModelError("Email", "An account with this email address already exists.");
                    }
                }
            }

            // Global Phone Uniqueness Check (cross-table / cross-role)
            if (!string.IsNullOrWhiteSpace(model.Phone) && (ModelState["Phone"] == null || ModelState["Phone"]!.Errors.Count == 0))
            {
                string cleanPhone = Regex.Replace(model.Phone, @"[\s\-\(\)\+]", "");

                bool phoneExistsInCompanies = await _context.Companies
                    .AnyAsync(c => c.Phone != null && (c.Phone == model.Phone || c.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "") == cleanPhone));

                bool phoneExistsInJobSeekers = await _context.JobSeekers
                    .AnyAsync(js => js.Phone != null && (js.Phone == model.Phone || js.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "") == cleanPhone));

                bool phoneExistsInUsers = await _userManager.Users
                    .AnyAsync(u => u.PhoneNumber != null && (u.PhoneNumber == model.Phone || u.PhoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "") == cleanPhone));

                if (phoneExistsInCompanies || phoneExistsInJobSeekers || phoneExistsInUsers)
                {
                    ModelState.AddModelError("Phone", "This phone number is already registered.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Create Identity User
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.Phone
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    // Convert duplicate email error into user-friendly message
                    if (error.Code == "DuplicateEmail" || error.Code == "DuplicateUserName")
                    {
                        ModelState.AddModelError("Email", "An account with this email address already exists.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }

                return View(model);
            }

            // Assign Role
            var roleResult = await _userManager.AddToRoleAsync(user, model.AccountType);

            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                await _userManager.DeleteAsync(user);
                return View(model);
            }

            // Create Profile Record
            try
            {
                if (model.AccountType == "Company")
                {
                    var company = new Company
                    {
                        CompanyName = model.CompanyName!,
                        Phone = model.Phone,
                        Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address,
                        Website = string.IsNullOrWhiteSpace(model.Website) ? null : model.Website,
                        Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description,
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
                        Phone = model.Phone,
                        Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address,
                        UserId = user.Id
                    };

                    _context.JobSeekers.Add(jobSeeker);
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Rollback user creation if constraint fails
                await _userManager.DeleteAsync(user);
                ModelState.AddModelError(string.Empty, "This email or phone number is already registered.");
                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);

            if (model.AccountType == "Company")
            {
                return RedirectToAction("Index", "Company");
            }
            else
            {
                return RedirectToAction("Index", "JobSeeker");
            }
        }

        // =========================================================
        // LOGIN
        // =========================================================

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectUserByRole(returnUrl);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email?.Trim() ?? string.Empty);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return RedirectUserByRole(returnUrl, user);
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "This account has been temporarily locked out. Please try again later.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // =========================================================
        // LOGOUT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // =========================================================
        // ACCESS DENIED
        // =========================================================

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // =========================================================
        // HELPER
        // =========================================================

        private IActionResult RedirectUserByRole(string? returnUrl = null, ApplicationUser? user = null)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (User.IsInRole("Company"))
            {
                return RedirectToAction("Index", "Company");
            }
            else if (User.IsInRole("JobSeeker"))
            {
                return RedirectToAction("Index", "JobSeeker");
            }

            return RedirectToAction("Index", "Home");
        }
    }
}