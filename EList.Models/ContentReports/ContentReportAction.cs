using EList.Models.Accounts;
using EList.Models.Enums;

namespace EList.Models.ContentReports
{
    public class ContentReportAction
    {
        public Guid Id { get; set; }
        public Guid ReportId { get; set; }
        public Guid? ActorAccountId { get; set; }
        public ReportActorContext ActorContext { get; set; }
        public string Action { get; set; }
        public string? Details { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public AccountPublicData? ActorAccount { get; set; }
    }
}
