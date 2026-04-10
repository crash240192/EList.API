namespace EList.Models.Events.EventMetadata
{
    /// <summary>
    /// Тип события
    /// </summary>
    public class EventTypeRequest
    {

        /// <summary>
        /// Маршрут до названия типа события
        /// </summary>
        public string NamePath { get; set; }

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
