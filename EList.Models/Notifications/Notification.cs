using Newtonsoft.Json.Linq;

namespace EList.Models.Notifications
{
    /// <summary>
    /// Модель запроса на отправку уведомления
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// Идентификатор уведомления
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// идентификатор связанного события
        /// </summary>
        public Guid? EventId { get; set; }
                
        /// <summary>
        /// Идентификатор аккаунта
        /// </summary>
        public Guid AccountId { get; set; }

        /// <summary>
        /// идентификатор аккаунта-инициатора уведомления
        /// </summary>
        public Guid? RelatedAccountId { get; set; }

        /// <summary>
        /// тип уведомления 
        /// </summary>
        public UserNotificationType? Type { get; set; }

        /// <summary>
        /// Заголовок уведомления
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Текст уведомления
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Дата создания уведомления
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// Дата просмотра уведомления
        /// </summary>
        public DateTimeOffset? ReadAt { get; set; }

        /// <summary>
        /// Произвольные данные (JSON-объект), передаваемые вместе с уведомлением (вспомогательная информация)
        /// </summary>
        public object? Data { get; set; }
    }
}
