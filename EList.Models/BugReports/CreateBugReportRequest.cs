namespace EList.Models.BugReports
{
    public class CreateBugReportRequest
    {
        /// <summary>
        /// Категория / раздел сайта
        /// </summary>
        public Guid CategoryId { get; set; }

        /// <summary>
        /// Описание проблемы
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Идентификаторы файлов скриншотов в filestorage
        /// </summary>
        public List<Guid>? FileIds { get; set; }
    }
}
