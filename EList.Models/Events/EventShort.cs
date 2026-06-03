using EList.Models.Events.EventMetadata;

namespace EList.Models.Events
{
    /// <summary>
    /// Событие
    /// </summary>
    public class EventShort
    {
        public EventShort() { }

        public EventShort(Event eventData) 
        {
            Id = eventData.Id;
            Name = eventData.Name;
            Latitude = eventData.Latitude;
            Longitude = eventData.Longitude;
            StartTime = eventData.StartTime;
            Colors = eventData.Types?.Select(i => i.EventCategory.Color)?.ToArray();
        }   

        /// <summary>
        /// Идентификатор
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Дата начала мероприятия
        /// </summary>
        public DateTimeOffset? StartTime { get; set; }

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
