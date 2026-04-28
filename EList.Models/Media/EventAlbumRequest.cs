namespace EList.Models.Media
{
    /// <summary>
    /// Тело запроса на создание альбома
    /// </summary>
    public class EventAlbumRequest
    {
        /// <summary>
        /// Идентификатор альбома
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// Название альбома
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Описание
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Идентификатор события (если это альбом для события)
        /// </summary>
        public Guid? EventId { get; set; }

        /// <summary>
        /// Идентификатор пользователя, создавшего альбом
        /// </summary>
        public Guid? AccountId { get; set; }

        /// <summary>
        /// Идентификатор организации-владельца альбома
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// Идентификатор файла обложки
        /// </summary>
        public Guid? WallpaperId { get; set; }

        /// <summary>
        /// Параметры альбоа
        /// </summary>
        public EventAlbumParameters Parameters { get; set; }
    }
}
