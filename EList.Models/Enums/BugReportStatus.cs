namespace EList.Models.Enums
{
    /// <summary>
    /// Статус багрепорта
    /// </summary>
    public enum BugReportStatus
    {
        /// <summary>
        /// Ожидает обработки
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Исправлено
        /// </summary>
        Resolved = 1,

        /// <summary>
        /// Отменено
        /// </summary>
        Cancelled = 2
    }
}
