namespace EList.Models.Events.EventMetadata
{
    /// <summary>
    /// Тип события
    /// </summary>
    public class EventTypeRequest
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
        /// Описание типа события
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Иконка
        /// </summary>
        public byte[] Ico { get; set; }

        /// <summary>
        /// Идентификатор категории
        /// </summary>
        public Guid EventCategoryId { get; set; }

    }
}
