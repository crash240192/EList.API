using EList.Models.Events.EventMetadata;

namespace EList.Models.Events
{
    /// <summary>
    /// Событие
    /// </summary>
    public class EventShort
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Название события
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Место проведения (широта)
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Место проведения (долгота)
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Перечень цветов категорий мероприятий
        /// </summary>
        public string[]? Colors { get; set; }
    }
}
