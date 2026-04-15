namespace EList.Models.Events.EventMetadata
{
    /// <summary>
    /// Запрос создания категории мероприятия
    /// </summary>
    public class EventCategoryRequest
    {

        /// <summary>
        /// Маршрут до локализации названия
        /// </summary>
        public string LocalizationPath { get; set; }

        /// <summary>
        /// Название
        /// </summary>
        public string Name { get; set; }

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
