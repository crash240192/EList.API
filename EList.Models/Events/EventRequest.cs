namespace EList.Models.Events
{
    /// <summary>
    /// Базовое тело запроса мероприятия
    /// </summary>
    public class EventRequest
    {
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
        /// Место проведения (Широта)
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Место проведения (Долгота)
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
        /// Идентификатор файла обложки
        /// </summary>
        public Guid? CoverImageId { get; set; }

        /// <summary>
        /// Горизонтальная точка фокуса обложки (0…100), для object-position
        /// </summary>
        public double? CoverFocusX { get; set; }

        /// <summary>
        /// Вертикальная точка фокуса обложки (0…100), для object-position
        /// </summary>
        public double? CoverFocusY { get; set; }
    }
}
