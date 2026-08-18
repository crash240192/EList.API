namespace EList.DbDataProvider.Models
{
    public class ContentReportTargetStatsDto
    {
        public int TotalReports { get; set; }
        public int OpenReports { get; set; }
        public int ResolvedReports { get; set; }
        public int DismissedReports { get; set; }
        public int WarningCount { get; set; }
        public DateTimeOffset? LastWarningAt { get; set; }
        public DateTimeOffset? LastReportAt { get; set; }
        public int RelatedTotalReports { get; set; }
        public int RelatedOpenReports { get; set; }
        public int RelatedWarningCount { get; set; }
    }
}
