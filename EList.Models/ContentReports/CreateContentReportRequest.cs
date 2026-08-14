using EList.Models.Enums;

namespace EList.Models.ContentReports
{
    public class CreateContentReportRequest
    {
        /// <summary>
        /// Тип цели: событие или сообщение в обсуждении
        /// </summary>
        public ReportTargetType TargetType { get; set; }

        /// <summary>
        /// Id события или сообщения
        /// </summary>
        public Guid TargetId { get; set; }

        /// <summary>
        /// Причина жалобы
        /// </summary>
        public Guid ReasonId { get; set; }

        /// <summary>
        /// Комментарий жалобщика
        /// </summary>
        public string? Comment { get; set; }
    }
}
