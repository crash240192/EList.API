namespace EList.Models.Events.EventMetadata
{
    /// <summary>
    /// Тип события
    /// </summary>
    public class EventType
    {
        /// <summary>
        /// Идентификатор типа события
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Название типа события
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Маршрут до названия типа события
        /// </summary>
        public string LocalizationPath { get; set; }

        /// <summary>
        /// Описание типа события
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Иконка
        /// </summary>
        public string Ico { get; set; }

        /// <summary>
        /// Идентификатор категории
        /// </summary>
        public Guid EventCategoryId { get; set; }

        /// <summary>
        /// Категория события
        /// </summary>
        public EventCategory EventCategory { get; set; }

    }
}
