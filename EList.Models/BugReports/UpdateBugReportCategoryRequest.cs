namespace EList.Models.BugReports
{
    public class UpdateBugReportCategoryRequest
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public int? SortOrder { get; set; }
        public bool? Active { get; set; }
    }
}
