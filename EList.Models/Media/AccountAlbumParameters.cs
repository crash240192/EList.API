namespace EList.Models.Media
{
    /// <summary>
    /// Параметры доступа к персональному альбому (не привязанному к событию).
    /// Структура зеркалит event_album_parameters для единообразной проверки доступа.
    /// </summary>
    public class AccountAlbumParameters
    {
        public Guid AlbumId { get; set; }
        public bool HeadAlbum { get; set; }
        public bool ParticipantsReadonly { get; set; }
        public bool Private { get; set; }
    }
}
