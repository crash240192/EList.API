using EList.Models.Enums;

namespace EList.Models.ContentReports
{
    /// <summary>
    /// Сводка жалоб и предупреждений по любой обжалуемой сущности.
    /// </summary>
    public class ContentReportTargetStats
    {
        public ReportTargetType TargetType { get; set; }
        public Guid TargetId { get; set; }

        /// <summary>Все жалобы, где эта сущность — прямая цель.</summary>
        public int TotalReports { get; set; }

        /// <summary>Открытые / в работе / эскалированные.</summary>
        public int OpenReports { get; set; }

        public int ResolvedReports { get; set; }
        public int DismissedReports { get; set; }

        /// <summary>Сколько раз по этой цели вынесено предупреждение (<c>Warn</c>).</summary>
        public int WarningCount { get; set; }

        public DateTimeOffset? LastWarningAt { get; set; }
        public DateTimeOffset? LastReportAt { get; set; }

        /// <summary>
        /// Связанные жалобы: для события — на сообщения/фото/организаторов этого события;
        /// для аккаунта — где он <c>reportedAccountId</c>; для организации — где она в <c>organizationId</c>.
        /// </summary>
        public int RelatedTotalReports { get; set; }
        public int RelatedOpenReports { get; set; }
        public int RelatedWarningCount { get; set; }

        public List<ModerationPenalty> ActivePenalties { get; set; } = new();
    }
}
