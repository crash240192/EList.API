namespace EList.Models.Media
{
    public class AlbumFile
    {
        /// <summary>
        /// Идентификатор записи
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Идентификатор файла
        /// </summary>
        public Guid FileId { get; set; }

        /// <summary>
        /// Идентификатор альбома
        /// </summary>
        public Guid AlbumId { get; set; }
    }
}
