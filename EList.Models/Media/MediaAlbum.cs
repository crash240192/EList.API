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
    }
}
