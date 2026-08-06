using EList.Models.Events;

namespace EList.Models.EventTemplates
{
    /// <summary>
    /// Запрос на создание шаблона мероприятия
    /// </summary>
    public class CreateEventTemplateRequest
    {
        /// <summary>
        /// Название шаблона
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Тело шаблона (запрос на создание мероприятия)
        /// </summary>
        public CreateEventRequest TemplateBody { get; set; }

        /// <summary>
        /// Идентификатор организации-владельца шаблона.
        /// Если не указан — шаблон создаётся для текущего пользователя.
        /// </summary>
        public Guid? OrganizationId { get; set; }
    }
}
