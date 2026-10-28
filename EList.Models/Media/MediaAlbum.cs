namespace EList.Models.Media
{
    /// <summary>
    /// Альбом медиа
    /// </summary>
    public class MediaAlbum
    {
        /// <summary>
        /// Идентификатор альбома
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Идентификатор создавшего альбом аккаунта
        /// </summary>
        public Guid AccountId { get; set; }

        /// <summary>
        /// Название альбома
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Описание
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Идентификатор ивента
        /// </summary>
        public Guid? EventId { get; set; }

        /// <summary>
        /// Дата создания
        /// </summary>
        public DateTimeOffset CreateDate { get; set; }

        /// <summary>
        /// Дата обновления
        /// </summary>
        public DateTimeOffset UpdateDate { get; set; }

        /// <summary>
        /// Идентификатор файла обложки
        /// </summary>
        public Guid? WallpaperId { get; set; }

        /// <summary>
        /// Параметры альбома мероприятия
        /// </summary>
        public EventAlbumParameters Parameters { get; set; }

        /// <summary>
        /// Параметры доступа к персональному альбому
        /// </summary>
        public AccountAlbumParameters AccountParameters { get; set; }

        /// <summary>Макс. create_date видимых файлов в альбоме (для «есть новое»).</summary>
        public DateTimeOffset? LastContentAt { get; set; }

        /// <summary>Когда текущий пользователь последний раз открывал альбом. null — ещё не смотрел.</summary>
        public DateTimeOffset? LastViewedAt { get; set; }

        /// <summary>Есть файлы новее LastViewedAt (или альбом не открывали при наличии контента).</summary>
        public bool HasNewContent { get; set; }

        /// <summary>Число видимых фото (для ленты альбомов без N+1 filesByAlbumId).</summary>
        public int? PhotoCount { get; set; }

        /// <summary>Первые fileId для мозаики/обложки (до 4).</summary>
        public List<Guid> CoverFileIds { get; set; }
    }
}
