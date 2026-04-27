namespace EList.Models.Media
{
    public class EventAlbumParameters
    {
        /// <summary>
        /// Идентификатор альбома
        /// </summary>
        public Guid AlbumId { get; set; }

        /// <summary>
        /// Флаг головного альбома ивента
        /// </summary>
        public bool HeadAlbum { get; set; }

        /// <summary>
        /// Флаг только для чтения (для участников)
        /// </summary>
        public bool ParticipantsRedonly { get; set; }
        
        /// <summary>
        /// Флаг закрытости альбома (виден только участникам события)
        /// </summary>
        public bool Private { get; set; }
    }
}
