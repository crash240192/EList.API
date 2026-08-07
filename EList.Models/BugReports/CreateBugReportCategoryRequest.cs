namespace EList.Models.BugReports
{
    public class CreateBugReportCategoryRequest
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public int SortOrder { get; set; } = 0;
    }
}
