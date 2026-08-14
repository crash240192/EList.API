using EList.Models.Enums;

namespace EList.Models.ContentReports
{
    public class CreateReportReasonRequest
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public ReportTargetScope TargetScope { get; set; } = ReportTargetScope.Both;
        public ReportSeverity Severity { get; set; }
        public ReportQueue PrimaryQueue { get; set; }
        public int SortOrder { get; set; }
    }
}
