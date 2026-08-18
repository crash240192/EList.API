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

        /// <summary>
        /// Для <c>ApplyPenalty</c> — какой запрет наложить.
        /// </summary>
        public ModerationPenaltyType? PenaltyType { get; set; }

        /// <summary>
        /// Срок ограничения в часах. null = бессрочно (до ручного снятия).
        /// Для ApplyPenalty обязателен, если не нужна бессрочная мера; минимум 1 час.
        /// Также можно передать вместе с SuspendAccount / SuspendOrganization / BanFromEvent.
        /// </summary>
        public int? DurationHours { get; set; }
    }
}
