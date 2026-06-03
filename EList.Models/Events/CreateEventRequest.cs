using EList.Models.Events.EventMetadata;

namespace EList.Models.Events
{
    /// <summary>
    /// Запрос на создание мероприятия
    /// </summary>
    public class CreateEventRequest
    {
        /// <summary>
        /// Информация о мероприятии
        /// </summary>
        public EventRequest Event { get; set; }

        /// <summary>
        /// Параметры мероприятия
        /// </summary>
        public EventParametersRequest? EventParameters { get; set; }

        /// <summary>
        /// Список типов к которым относится мероприятие
        /// </summary>
        public List<Guid> EventTypes { get; set; }

        /// <summary>
        /// Список идентификаторов аккаунтов-организаторов мероприятия
        /// </summary>
        public List<Guid> OrganizatorAccountIds { get; set; }

        /// <summary>
        /// Список идентификаторов организаций-организаторов мероприятия
        /// </summary>
        public List<Guid> OrganizatorOrganizationIds { get; set; }

        /// <summary>
        /// Автоприглашение всех подписчиков
        /// </summary>
        public bool InviteAllSubscribers { get; set; } = false;

        /// <summary>
        /// Список пользователей, которым нужно выслать приглашения
        /// </summary>
        public List<Guid> InviteUsers { get; set; }

        /// <summary>
        /// Черный список участников
        /// </summary>
        public List<Guid> BlackList { get; set; }

        /// <summary>
        /// Белый список участников
        /// </summary>
        public List<Guid> WhiteList { get; set; }
    }
}
