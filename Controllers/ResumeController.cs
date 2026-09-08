using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkAt.Data;
using WorkAt.Models;

namespace WorkAt.Controllers
{
    [Authorize(Roles = "JobSeeker")]
    public class ResumeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ResumeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Resume
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var jobSeeker = await _context.JobSeekers
                .Include(js => js.Resume)
                    .ThenInclude(r => r!.ResumeSkills)
                        .ThenInclude(rs => rs.Skill)
                .FirstOrDefaultAsync(js => js.UserId == userId);

            if (jobSeeker == null)
            {
                return NotFound();
            }

            return View(jobSeeker.Resume);
        }

        // GET: /Resume/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Resume/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Resume resume)
        {
            var userId = _userManager.GetUserId(User);

            var jobSeeker = await _context.JobSeekers
                .FirstOrDefaultAsync(js => js.UserId == userId);

            if (jobSeeker == null)
            {
                return NotFound();
            }

            // A JobSeeker can have only one resume.
            var existingResume = await _context.Resumes
                .FirstOrDefaultAsync(r =>
                    r.JobSeekerId == jobSeeker.JobSeekerId);

            if (existingResume != null)
            {
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                return View(resume);
            }

            // Assign the resume to the logged-in JobSeeker.
            resume.JobSeekerId = jobSeeker.JobSeekerId;

            _context.Resumes.Add(resume);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Resume/Edit
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = _userManager.GetUserId(User);

            var jobSeeker = await _context.JobSeekers
                .Include(js => js.Resume)
                .FirstOrDefaultAsync(js => js.UserId == userId);

            if (jobSeeker == null)
            {
                return NotFound();
            }

            if (jobSeeker.Resume == null)
            {
                return RedirectToAction(nameof(Create));
            }

            return View(jobSeeker.Resume);
        }

        // POST: /Resume/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Resume resume)
        {
            var userId = _userManager.GetUserId(User);

            var jobSeeker = await _context.JobSeekers
                .Include(js => js.Resume)
                .FirstOrDefaultAsync(js => js.UserId == userId);

            if (jobSeeker == null)
            {
                return NotFound();
            }

            if (jobSeeker.Resume == null)
            {
                return RedirectToAction(nameof(Create));
            }

            if (!ModelState.IsValid)
            {
                return View(resume);
            }

            // Get the actual resume belonging to the logged-in user.
            var existingResume = jobSeeker.Resume;

            existingResume.Summary = resume.Summary;
            existingResume.Education = resume.Education;
            existingResume.Experience = resume.Experience;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Resume/AddSkill
        [HttpGet]
        public async Task<IActionResult> AddSkill()
        {
            var userId = _userManager.GetUserId(User);

            var jobSeeker = await _context.JobSeekers
                .Include(js => js.Resume)
                .FirstOrDefaultAsync(js => js.UserId == userId);

            if (jobSeeker == null)
            {
                return NotFound();
            }

            if (jobSeeker.Resume == null)
            {
                return RedirectToAction(nameof(Create));
            }

            var skills = await _context.Skills
                .OrderBy(s => s.Name)
                .ToListAsync();

            ViewBag.Skills = skills;

            return View();
        }

        // POST: /Resume/AddSkill
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSkill(int skillId)
        {
            var userId = _userManager.GetUserId(User);

            var jobSeeker = await _context.JobSeekers
                .Include(js => js.Resume)
                .FirstOrDefaultAsync(js => js.UserId == userId);

            if (jobSeeker == null)
            {
                return NotFound();
            }

            if (jobSeeker.Resume == null)
            {
                return RedirectToAction(nameof(Create));
            }

            // Check whether the skill exists.
            var skill = await _context.Skills
                .FindAsync(skillId);

            if (skill == null)
            {
                return NotFound();
            }

            // Prevent adding the same skill twice.
            var alreadyAdded = await _context.ResumeSkills
                .AnyAsync(rs =>
                    rs.ResumeId == jobSeeker.Resume.ResumeId &&
                    rs.SkillId == skillId);

            if (!alreadyAdded)
            {
                var resumeSkill = new ResumeSkill
                {
                    ResumeId = jobSeeker.Resume.ResumeId,
                    SkillId = skillId
                };

                _context.ResumeSkills.Add(resumeSkill);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Resume/RemoveSkill
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveSkill(int skillId)
        {
            var userId = _userManager.GetUserId(User);

            var jobSeeker = await _context.JobSeekers
                .Include(js => js.Resume)
                .FirstOrDefaultAsync(js => js.UserId == userId);

            if (jobSeeker == null)
            {
                return NotFound();
            }

            if (jobSeeker.Resume == null)
            {
                return RedirectToAction(nameof(Create));
            }

            var resumeSkill = await _context.ResumeSkills
                .FirstOrDefaultAsync(rs =>
                    rs.ResumeId == jobSeeker.Resume.ResumeId &&
                    rs.SkillId == skillId);

            if (resumeSkill != null)
            {
                _context.ResumeSkills.Remove(resumeSkill);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}