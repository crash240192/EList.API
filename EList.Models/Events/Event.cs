using EList.Models.Events.EventMetadata;

namespace EList.Models.Events
{
    /// <summary>
    /// Событие
    /// </summary>
    public class Event
    {
        /// <summary>
        /// Идентификатор
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Дата начала события
        /// </summary>
        public DateTimeOffset StartTime { get; set; }

        /// <summary>
        /// Дата окончания события
        /// </summary>
        public DateTimeOffset EndTime { get; set; }
        
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
        /// Описание
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Адрес
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Флаг вкл/выкл
        /// </summary>
        public bool Active { get; set; }

        /// <summary>
        /// Указатель на параметры события
        /// </summary>
        public Guid? EventParametersId { get; set; }

        /// <summary>
        /// Дата создания
        /// </summary>
        public DateTimeOffset CreationDate { get; set; }

        /// <summary>
        /// Дата последнего обновления
        /// </summary>
        public DateTimeOffset UpdateDate { get; set; }

        /// <summary>
        /// Идентификатор файла обложки
        /// </summary>
        public Guid? CoverImageId { get; set; }

        /// <summary>
        /// Дополнительные параметры мероприятия
        /// </summary>
        public EventParameters Parameters { get; set; }

        /// <summary>
        /// Список типов мероприятий
        /// </summary>
        public List<EventType> Types { get; set; }
    }
}
