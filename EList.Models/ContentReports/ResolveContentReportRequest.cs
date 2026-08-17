using EList.Models.Enums;

namespace EList.Models.ContentReports
{
    public class ResolveContentReportRequest
    {
        public ReportResolutionAction ResolutionAction { get; set; }
        public string? ResolutionComment { get; set; }

        /// <summary>
        /// Для ban_from_event / suspend_account — id аккаунта (если не из snapshot)
        /// </summary>
        public Guid? TargetAccountId { get; set; }
    }
}
