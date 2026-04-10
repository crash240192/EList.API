namespace EList.Models.Events.EventMetadata
{
    /// <summary>
    /// Запрос создания категории мероприятия
    /// </summary>
    public class EventCategoryRequest
    {

        /// <summary>
        /// Маршрут до названия
        /// </summary>
        public string NamePath { get; set; }

        /// <summary>
        /// Иконка
        /// </summary>
        public byte[] Ico { get; set; }

        /// <summary>
        /// Описание
        /// </summary>
        public string Description { get; set; }
    }
}
