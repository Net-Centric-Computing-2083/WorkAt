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

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }
}
