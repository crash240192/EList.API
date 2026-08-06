using EList.Models.Events;

namespace EList.Models.EventTemplates
{
    /// <summary>
    /// Запрос на обновление шаблона мероприятия
    /// </summary>
    public class UpdateEventTemplateRequest
    {
        /// <summary>
        /// Название шаблона
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Тело шаблона (запрос на создание мероприятия)
        /// </summary>
        public CreateEventRequest? TemplateBody { get; set; }
    }
}
