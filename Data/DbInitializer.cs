using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkAt.Models;

namespace WorkAt.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Ensure Roles Exist
            string[] roles = { "Admin", "Company", "JobSeeker" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }



            // 3. Seed Skills
            var defaultSkills = new[]
            {
                "C#", "ASP.NET Core", ".NET Core", "Entity Framework Core", "SQL Server", "PostgreSQL",
                "React", "Angular", "Vue.js", "TypeScript", "JavaScript", "Python", "Django", "FastAPI",
                "Docker", "Kubernetes", "AWS", "Azure", "Git", "REST APIs", "GraphQL", "Tailwind CSS",
                "Bootstrap", "UI/UX Design", "Machine Learning", "Data Analysis", "Agile/Scrum"
            };

            foreach (var skillName in defaultSkills)
            {
                if (!await context.Skills.AnyAsync(s => s.Name.ToLower() == skillName.ToLower()))
                {
                    context.Skills.Add(new Skill { Name = skillName });
                }
            }
            await context.SaveChangesAsync();

            var allSkills = await context.Skills.ToListAsync();

            // 4. Seed Companies
            var seedCompanies = new[]
            {
                new {
                    Email = "contact@technova.io",
                    Password = "Company@123",
                    Phone = "9841112233",
                    CompanyName = "TechNova Solutions",
                    Address = "Kathmandu, Bagmati, Nepal",
                    Website = "https://technova.example.com",
                    Description = "Leading enterprise cloud and full-stack software development firm delivering mission-critical applications.",
                    Status = "Verified"
                },
                new {
                    Email = "hr@cloudpeak.dev",
                    Password = "Company@123",
                    Phone = "9841223344",
                    CompanyName = "CloudPeak Innovations",
                    Address = "Pulchowk, Lalitpur, Nepal",
                    Website = "https://cloudpeak.example.com",
                    Description = "Pioneering DevOps, cloud architecture, and high-performance distributed systems for international clients.",
                    Status = "Verified"
                },
                new {
                    Email = "talent@apexailabs.com",
                    Password = "Company@123",
                    Phone = "9841334455",
                    CompanyName = "Apex AI Labs",
                    Address = "Lakeside, Pokhara, Nepal",
                    Website = "https://apexai.example.com",
                    Description = "Cutting-edge artificial intelligence, computer vision, and applied machine learning research laboratory.",
                    Status = "Verified"
                },
                new {
                    Email = "careers@himalayanfintech.com",
                    Password = "Company@123",
                    Phone = "9841445566",
                    CompanyName = "Himalayan Fintech",
                    Address = "New Baneshwor, Kathmandu, Nepal",
                    Website = "https://himalayanfintech.example.com",
                    Description = "Next-generation digital payment gateway and micro-lending infrastructure for South Asian markets.",
                    Status = "Pending"
                },
                new {
                    Email = "sec@cybershield.io",
                    Password = "Company@123",
                    Phone = "9841556677",
                    CompanyName = "CyberShield Security",
                    Address = "Suryabinayak, Bhaktapur, Nepal",
                    Website = "https://cybershield.example.com",
                    Description = "Offensive security testing, SOC operations, and regulatory compliance consulting.",
                    Status = "Rejected"
                }
            };

            var companyEntities = new List<Company>();

            foreach (var item in seedCompanies)
            {
                var existingUser = await userManager.FindByEmailAsync(item.Email);
                Company? companyRecord = null;

                if (existingUser == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = item.Email,
                        Email = item.Email,
                        PhoneNumber = item.Phone,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, item.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "Company");

                        companyRecord = new Company
                        {
                            CompanyName = item.CompanyName,
                            Phone = item.Phone,
                            Address = item.Address,
                            Website = item.Website,
                            Description = item.Description,
                            UserId = user.Id,
                            Status = item.Status,
                            CreatedAt = DateTime.UtcNow.AddDays(-new Random().Next(10, 45))
                        };
                        context.Companies.Add(companyRecord);
                        await context.SaveChangesAsync();
                    }
                }
                else
                {
                    companyRecord = await context.Companies.FirstOrDefaultAsync(c => c.UserId == existingUser.Id);
                }

                if (companyRecord != null)
                {
                    companyEntities.Add(companyRecord);
                }
            }

            // 5. Seed Job Seekers with Resumes and Skills
            var seedJobSeekers = new[]
            {
                new {
                    Email = "aarav.sharma@example.com",
                    Password = "Seeker@123",
                    Phone = "9812000001",
                    FirstName = "Aarav",
                    LastName = "Sharma",
                    Address = "Baneshwor, Kathmandu",
                    Status = "Verified",
                    Summary = "Results-driven Full Stack .NET Developer with 3+ years of experience building secure web APIs and scalable cloud microservices.",
                    Education = "Bachelor of Science in Computer Science and Information Technology (BSc. CSIT) - Tribhuvan University (2020 - 2024)",
                    Experience = "Software Engineer at InfoTech Nepal (2024 - Present): Built ASP.NET Core microservices and React dashboards.\nJunior Developer at WebCore (2023 - 2024): Developed REST APIs and Entity Framework queries.",
                    Skills = new[] { "C#", "ASP.NET Core", "SQL Server", "React", "Docker", "Git" }
                },
                new {
                    Email = "pooja.thapa@example.com",
                    Password = "Seeker@123",
                    Phone = "9812000002",
                    FirstName = "Pooja",
                    LastName = "Thapa",
                    Address = "Kupondole, Lalitpur",
                    Status = "Verified",
                    Summary = "Data Scientist & Machine Learning practitioner experienced in statistical modeling, NLP pipelines, and interactive data visualization.",
                    Education = "Master of Information Technology - Kathmandu University (2022 - 2024)\nB.E. in Computer Engineering - IOE Pulchowk (2018 - 2022)",
                    Experience = "Data Analyst at DataMind Labs (2023 - Present): Formulated predictive models for customer churn using Python and Scikit-Learn.",
                    Skills = new[] { "Python", "Machine Learning", "Data Analysis", "SQL Server", "FastAPI" }
                },
                new {
                    Email = "rohan.shrestha@example.com",
                    Password = "Seeker@123",
                    Phone = "9812000003",
                    FirstName = "Rohan",
                    LastName = "Shrestha",
                    Address = "Lakeside, Pokhara",
                    Status = "Verified",
                    Summary = "Frontend Developer passionate about crafting pixel-perfect, accessible user interfaces with React, TypeScript, and modern CSS architectures.",
                    Education = "Bachelor of Computer Application (BCA) - Pokhara University (2021 - 2025)",
                    Experience = "Frontend Intern at CreativeHub (2024): Built responsive UI components with React and Tailwind CSS.",
                    Skills = new[] { "React", "TypeScript", "JavaScript", "Tailwind CSS", "UI/UX Design", "Git" }
                },
                new {
                    Email = "sneha.adhikari@example.com",
                    Password = "Seeker@123",
                    Phone = "9812000004",
                    FirstName = "Sneha",
                    LastName = "Adhikari",
                    Address = "Koteshwor, Kathmandu",
                    Status = "Pending",
                    Summary = "DevOps enthusiast specializing in Kubernetes orchestration, CI/CD automated deployments, and cloud infrastructure automation.",
                    Education = "B.Tech in Information Technology - Tribhuvan University (2020 - 2024)",
                    Experience = "DevOps Trainee at CloudNepal (2024): Implemented GitHub Actions CI pipelines and Dockerized microservices.",
                    Skills = new[] { "Docker", "Kubernetes", "Azure", "AWS", "Git" }
                },
                new {
                    Email = "bikash.gurung@example.com",
                    Password = "Seeker@123",
                    Phone = "9812000005",
                    FirstName = "Bikash",
                    LastName = "Gurung",
                    Address = "Bharatpur, Chitwan",
                    Status = "Verified",
                    Summary = "Backend Engineer with strong fundamentals in relational database optimization, RESTful web services, and distributed caching.",
                    Education = "Bachelor of Software Engineering - Gandaki University (2020 - 2024)",
                    Experience = "Backend Developer at SwiftSolutions (2023 - Present): Engineered backend APIs with ASP.NET Core and PostgreSQL.",
                    Skills = new[] { "C#", "ASP.NET Core", "REST APIs", "Entity Framework Core", "PostgreSQL" }
                }
            };

            var jobSeekerEntities = new List<JobSeeker>();

            foreach (var item in seedJobSeekers)
            {
                var existingUser = await userManager.FindByEmailAsync(item.Email);
                JobSeeker? jsRecord = null;

                if (existingUser == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = item.Email,
                        Email = item.Email,
                        PhoneNumber = item.Phone,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, item.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "JobSeeker");

                        jsRecord = new JobSeeker
                        {
                            FirstName = item.FirstName,
                            LastName = item.LastName,
                            Phone = item.Phone,
                            Address = item.Address,
                            UserId = user.Id,
                            Status = item.Status,
                            CreatedAt = DateTime.UtcNow.AddDays(-new Random().Next(5, 30))
                        };
                        context.JobSeekers.Add(jsRecord);
                        await context.SaveChangesAsync();

                        // Add Resume
                        var resume = new Resume
                        {
                            JobSeekerId = jsRecord.JobSeekerId,
                            Summary = item.Summary,
                            Education = item.Education,
                            Experience = item.Experience
                        };
                        context.Resumes.Add(resume);
                        await context.SaveChangesAsync();

                        // Add Resume Skills
                        foreach (var skillName in item.Skills)
                        {
                            var skillObj = allSkills.FirstOrDefault(s => s.Name.ToLower() == skillName.ToLower());
                            if (skillObj != null)
                            {
                                context.ResumeSkills.Add(new ResumeSkill
                                {
                                    ResumeId = resume.ResumeId,
                                    SkillId = skillObj.SkillId
                                });
                            }
                        }
                        await context.SaveChangesAsync();
                    }
                }
                else
                {
                    jsRecord = await context.JobSeekers
                        .Include(js => js.Resume)
                        .FirstOrDefaultAsync(js => js.UserId == existingUser.Id);
                }

                if (jsRecord != null)
                {
                    jobSeekerEntities.Add(jsRecord);
                }
            }

            // 6. Seed Jobs for Verified Companies
            var techNova = companyEntities.FirstOrDefault(c => c.CompanyName == "TechNova Solutions");
            var cloudPeak = companyEntities.FirstOrDefault(c => c.CompanyName == "CloudPeak Innovations");
            var apexAi = companyEntities.FirstOrDefault(c => c.CompanyName == "Apex AI Labs");

            var seedJobs = new List<Job>();

            if (techNova != null && !await context.Jobs.AnyAsync(j => j.CompanyId == techNova.CompanyId))
            {
                seedJobs.Add(new Job
                {
                    Title = "Senior Full-Stack .NET Developer",
                    Description = "We are seeking a talented Senior .NET Developer to build resilient enterprise backends and modern single-page frontend interfaces.",
                    Requirements = "- 4+ years of C# / ASP.NET Core experience\n- Experience with React or Angular\n- Strong database design & EF Core skills\n- Excellent problem solving and communication",
                    Location = "Kathmandu / Hybrid",
                    Salary = "NPR 120,000 - 180,000 / month",
                    EmploymentType = "Full-Time",
                    CompanyId = techNova.CompanyId,
                    PostedDate = DateTime.UtcNow.AddDays(-12),
                    Deadline = DateTime.UtcNow.AddDays(18)
                });

                seedJobs.Add(new Job
                {
                    Title = "Cloud & DevOps Specialist",
                    Description = "Lead the migration and optimization of our microservices infrastructure on AWS and Azure with automated Kubernetes pipelines.",
                    Requirements = "- Proven track record in Docker, Kubernetes, and Helm\n- Deep knowledge of CI/CD pipelines (GitHub Actions, GitLab)\n- Cloud certification (AWS Solutions Architect / Azure Administrator) is a plus",
                    Location = "Kathmandu / Remote",
                    Salary = "NPR 140,000 - 200,000 / month",
                    EmploymentType = "Full-Time",
                    CompanyId = techNova.CompanyId,
                    PostedDate = DateTime.UtcNow.AddDays(-8),
                    Deadline = DateTime.UtcNow.AddDays(22)
                });
            }

            if (cloudPeak != null && !await context.Jobs.AnyAsync(j => j.CompanyId == cloudPeak.CompanyId))
            {
                seedJobs.Add(new Job
                {
                    Title = "Frontend React Engineer",
                    Description = "Design and implement intuitive, responsive, and blazing-fast user interfaces for our real-time analytics suite.",
                    Requirements = "- 2+ years of production experience with React and TypeScript\n- Solid understanding of state management (Redux Toolkit / Zustand)\n- Modern CSS frameworks (Tailwind CSS, CSS Modules)",
                    Location = "Lalitpur",
                    Salary = "NPR 80,000 - 120,000 / month",
                    EmploymentType = "Full-Time",
                    CompanyId = cloudPeak.CompanyId,
                    PostedDate = DateTime.UtcNow.AddDays(-6),
                    Deadline = DateTime.UtcNow.AddDays(14)
                });

                seedJobs.Add(new Job
                {
                    Title = "Junior Backend Developer (C# / .NET)",
                    Description = "Great opportunity for an enthusiastic junior developer to learn from experienced architects while building scalable APIs.",
                    Requirements = "- Understanding of OOP concepts and C# fundamentals\n- Basic experience with ASP.NET Core and SQL databases\n- Willingness to learn and grow in an agile environment",
                    Location = "Remote",
                    Salary = "NPR 50,000 - 75,000 / month",
                    EmploymentType = "Full-Time",
                    CompanyId = cloudPeak.CompanyId,
                    PostedDate = DateTime.UtcNow.AddDays(-4),
                    Deadline = DateTime.UtcNow.AddDays(25)
                });
            }

            if (apexAi != null && !await context.Jobs.AnyAsync(j => j.CompanyId == apexAi.CompanyId))
            {
                seedJobs.Add(new Job
                {
                    Title = "AI / Machine Learning Engineer",
                    Description = "Develop and deploy computer vision and large language model features into client-facing platforms.",
                    Requirements = "- Strong Python programming skills (PyTorch / TensorFlow / HuggingFace)\n- Experience deploying ML models via FastAPI or Triton Inference Server\n- Background in Mathematics, Statistics, or Computer Science",
                    Location = "Pokhara / Remote",
                    Salary = "NPR 150,000 - 220,000 / month",
                    EmploymentType = "Full-Time",
                    CompanyId = apexAi.CompanyId,
                    PostedDate = DateTime.UtcNow.AddDays(-10),
                    Deadline = DateTime.UtcNow.AddDays(20)
                });

                seedJobs.Add(new Job
                {
                    Title = "Data Science Intern",
                    Description = "Hands-on internship program focusing on exploratory data analysis, feature engineering, and data pipeline construction.",
                    Requirements = "- Passion for data and analytics\n- Familiarity with Python, Pandas, and SQL\n- Currently enrolled in or recent graduate in CS/IT or related STEM field",
                    Location = "Remote",
                    Salary = "NPR 25,000 - 35,000 / month",
                    EmploymentType = "Internship",
                    CompanyId = apexAi.CompanyId,
                    PostedDate = DateTime.UtcNow.AddDays(-3),
                    Deadline = DateTime.UtcNow.AddDays(30)
                });
            }

            if (seedJobs.Any())
            {
                context.Jobs.AddRange(seedJobs);
                await context.SaveChangesAsync();
            }

            // 7. Seed Applications and Feedbacks
            var allJobs = await context.Jobs.Include(j => j.Company).ToListAsync();
            var allJobSeekers = await context.JobSeekers.ToListAsync();

            if (allJobs.Any() && allJobSeekers.Any() && !await context.Applications.AnyAsync())
            {
                var aarav = allJobSeekers.FirstOrDefault(js => js.FirstName == "Aarav");
                var pooja = allJobSeekers.FirstOrDefault(js => js.FirstName == "Pooja");
                var rohan = allJobSeekers.FirstOrDefault(js => js.FirstName == "Rohan");
                var bikash = allJobSeekers.FirstOrDefault(js => js.FirstName == "Bikash");

                var seniorDotNetJob = allJobs.FirstOrDefault(j => j.Title.Contains("Senior Full-Stack"));
                var cloudDevOpsJob = allJobs.FirstOrDefault(j => j.Title.Contains("DevOps"));
                var reactJob = allJobs.FirstOrDefault(j => j.Title.Contains("React"));
                var mlJob = allJobs.FirstOrDefault(j => j.Title.Contains("Machine Learning"));
                var juniorDotNetJob = allJobs.FirstOrDefault(j => j.Title.Contains("Junior Backend"));

                var apps = new List<Application>();

                if (aarav != null && seniorDotNetJob != null)
                {
                    apps.Add(new Application
                    {
                        JobId = seniorDotNetJob.JobId,
                        JobSeekerId = aarav.JobSeekerId,
                        Status = "Shortlisted",
                        AppliedDate = DateTime.UtcNow.AddDays(-7),
                        Feedback = new ApplicationFeedback
                        {
                            FeedbackText = "Impressive background in ASP.NET Core and clean code principles. Moving forward to technical interview.",
                            FeedbackDate = DateTime.UtcNow.AddDays(-5)
                        }
                    });
                }

                if (rohan != null && reactJob != null)
                {
                    apps.Add(new Application
                    {
                        JobId = reactJob.JobId,
                        JobSeekerId = rohan.JobSeekerId,
                        Status = "Accepted",
                        AppliedDate = DateTime.UtcNow.AddDays(-5),
                        Feedback = new ApplicationFeedback
                        {
                            FeedbackText = "Excellent frontend portfolio and great cultural fit. Offer letter has been emailed.",
                            FeedbackDate = DateTime.UtcNow.AddDays(-2)
                        }
                    });
                }

                if (pooja != null && mlJob != null)
                {
                    apps.Add(new Application
                    {
                        JobId = mlJob.JobId,
                        JobSeekerId = pooja.JobSeekerId,
                        Status = "UnderReview",
                        AppliedDate = DateTime.UtcNow.AddDays(-3),
                        Feedback = new ApplicationFeedback
                        {
                            FeedbackText = "Resume matches requirements well. Reviewing previous research publications.",
                            FeedbackDate = DateTime.UtcNow.AddDays(-1)
                        }
                    });
                }

                if (bikash != null && juniorDotNetJob != null)
                {
                    apps.Add(new Application
                    {
                        JobId = juniorDotNetJob.JobId,
                        JobSeekerId = bikash.JobSeekerId,
                        Status = "Submitted",
                        AppliedDate = DateTime.UtcNow.AddDays(-1)
                    });
                }

                if (aarav != null && cloudDevOpsJob != null)
                {
                    apps.Add(new Application
                    {
                        JobId = cloudDevOpsJob.JobId,
                        JobSeekerId = aarav.JobSeekerId,
                        Status = "Pending",
                        AppliedDate = DateTime.UtcNow.AddDays(-2)
                    });
                }

                if (apps.Any())
                {
                    context.Applications.AddRange(apps);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
