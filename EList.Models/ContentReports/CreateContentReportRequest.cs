using EList.Models.Enums;

namespace EList.Models.ContentReports
{
    public class CreateContentReportRequest
    {
        /// <summary>
        /// Тип цели: событие, сообщение, фото, аккаунт, организация или организатор мероприятия
        /// </summary>
        public ReportTargetType TargetType { get; set; }

        /// <summary>
        /// Id цели: события, сообщения, файла, аккаунта, организации или записи event_organizators
        /// </summary>
        public Guid TargetId { get; set; }

        /// <summary>
        /// Для жалобы на фото в альбоме — id альбома (опционально, если файл однозначен)
        /// </summary>
        public Guid? AlbumId { get; set; }

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
