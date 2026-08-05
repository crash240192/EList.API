using EList.Models.Enums;
using EList.Models.Events.EventMetadata;

namespace EList.Models.Events
{
    /// <summary>
    /// Тело запроса поиска мероприятий
    /// </summary>
    public class EventsSearchRequest
    {
        /// <summary>
        /// Дата начала выборки
        /// </summary>
        public DateTimeOffset? StartTime { get; set; }

        /// <summary>
        /// Дата окончания выборки
        /// </summary>
        public DateTimeOffset? EndTime { get; set; }

        /// <summary>
        /// Локация (широта)
        /// В случае отсутствия locationRange ищем внутри города по указанным координатам
        /// </summary>
        public double? Latitude { get; set; }

        /// <summary>
        /// Локация (долгота)
        /// В случае отсутствия locationRange ищем внутри города по указанным координатам
        /// </summary>
        public double? Longitude{ get; set; }

        /// <summary>
        /// Размер зоны выборки
        /// </summary>
        public int? LocationRange { get; set; }

        /// <summary>
        /// Список типов мероприятий
        /// </summary>
        public List<Guid> Types { get; set; }

        /// <summary>
        /// Список категорий мероприятий
        /// </summary>
        public List<Guid> Categories { get; set; }

        /// <summary>
        /// Подстрока названия мероприятия
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Поиск мероприятий указанного аккаунта организатора
        /// (включая мероприятия организаций, где этот аккаунт — активный участник)
        /// </summary>
        public Guid? OrganizatorId { get; set; }

        /// <summary>
        /// Поиск мероприятий указанной организации
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// Поиск мероприятий в которых участвует указанный аккаунт
        /// </summary>
        public Guid? ParticipantId { get; set; }

        /// <summary>
        /// Стоимость мероприятия
        /// </summary>
        public double? Price { get; set; }

        /// <summary>
        /// Мальчишник/девишник
        /// </summary>
        public Gender? AllowedGender { get; set; }

        /// <summary>
        /// Возрастной порог
        /// </summary>
        public int? AgeLimit { get; set; }

        /// <summary>
        /// Отображать только мероприятия для взрослых
        /// </summary>
        public bool AdultOnly { get; set; } = false;

        /// <summary>
        /// Состояние мероприятия
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Номер страницы
        /// </summary>
        public int? PageIndex { get; set; } = 0;

        /// <summary>
        /// Размер страницы
        /// </summary>
        public int? PageSize { get; set; } = 20;

        /// <summary>
        /// Порядок сортировки
        /// </summary>
        public string OrderBy { get; set; }

        /// <summary>
        /// Поиск только мероприятий с привязанными альбомами
        /// </summary>
        public bool OnlyWithAlbums { get; set; } = false;
    }
}
