namespace EList.Models.Media
{
    /// <summary>
    /// Тело запроса на создание альбома
    /// </summary>
    public class CreateAlbumRequest
    {
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
    }
}
