namespace EList.Models.BugReports
{
    public class BugReportCategory
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public bool Active { get; set; }
        public int SortOrder { get; set; }
        public DateTimeOffset CreateDate { get; set; }
    }
}
