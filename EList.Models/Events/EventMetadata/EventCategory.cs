namespace EList.Models.Events.EventMetadata
{
    /// <summary>
    /// Категория (группа типов) мероприятия
    /// </summary>
    public class EventCategory
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Название категории
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Маршрут до названия
        /// </summary>
        public string LocalizationPath { get; set; }

        /// <summary>
        /// Иконка
        /// </summary>
        public byte[] Ico { get; set; }

        /// <summary>
        /// Описание
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Цвет категории
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// Активна ли категория (soft-delete)
        /// </summary>
        public bool Active { get; set; } = true;
    }
}
