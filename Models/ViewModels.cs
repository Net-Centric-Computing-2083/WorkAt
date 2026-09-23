namespace WorkAt.Models
{
    public class MyApplicationsViewModel
    {
        public IEnumerable<Application> Applications { get; set; } = new List<Application>();
        public int PageIndex { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; } = 0;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? SortOrder { get; set; } = "desc";

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }

    public class CompanyJobsViewModel
    {
        public IEnumerable<Job> Jobs { get; set; } = new List<Job>();
        public int PageIndex { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; } = 0;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public string? EmploymentType { get; set; }
        public string? SortOrder { get; set; } = "desc";

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }

    public class CompanyApplicationsViewModel
    {
        public IEnumerable<Application> Applications { get; set; } = new List<Application>();
        public int PageIndex { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; } = 0;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? SortOrder { get; set; } = "desc";

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }

    public class BrowseJobsViewModel
    {
        public IEnumerable<Job> Jobs { get; set; } = new List<Job>();
        public int PageIndex { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; } = 0;
        public int PageSize { get; set; } = 9;
        public string? Keyword { get; set; }
        public string? Location { get; set; }
        public string? EmploymentType { get; set; }
        public string? Salary { get; set; }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }

    public class AdminDashboardViewModel
    {
        // Company Statistics
        public int TotalCompanies { get; set; }
        public int PendingCompanies { get; set; }
        public int AcceptedCompanies { get; set; }
        public int RejectedCompanies { get; set; }

        // Job Seeker Statistics
        public int TotalJobSeekers { get; set; }
        public int PendingJobSeekers { get; set; }
        public int AcceptedJobSeekers { get; set; }
        public int RejectedJobSeekers { get; set; }

        // Job Statistics
        public int TotalJobs { get; set; }
        public int ActiveJobs { get; set; }
        public int ExpiredJobs { get; set; }

        // Application Statistics
        public int TotalApplications { get; set; }
        public int PendingApplications { get; set; }
        public int AcceptedApplications { get; set; }
        public int RejectedApplications { get; set; }

        // Queues & Recent Records
        public IEnumerable<Company> RecentPendingCompanies { get; set; } = new List<Company>();
        public IEnumerable<JobSeeker> RecentPendingJobSeekers { get; set; } = new List<JobSeeker>();
        public IEnumerable<Application> RecentApplications { get; set; } = new List<Application>();
    }

    public class AdminCompaniesViewModel
    {
        public IEnumerable<Company> Companies { get; set; } = new List<Company>();
        public int PageIndex { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; } = 0;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public string? Status { get; set; }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }

    public class AdminJobSeekersViewModel
    {
        public IEnumerable<JobSeeker> JobSeekers { get; set; } = new List<JobSeeker>();
        public int PageIndex { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; } = 0;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public string? Status { get; set; }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }

    public class AdminApplicationsViewModel
    {
        public IEnumerable<Application> Applications { get; set; } = new List<Application>();
        public int PageIndex { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; } = 0;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public string? Status { get; set; }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }

    public class ApplyJobViewModel
    {
        public int JobId { get; set; }
        public Job? Job { get; set; }
        public IFormFile? ResumeFile { get; set; }
    }
}
