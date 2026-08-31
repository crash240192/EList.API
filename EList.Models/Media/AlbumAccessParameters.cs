namespace EList.Models.Media
{
    /// <summary>
    /// Унифицированные параметры доступа к альбому для валидатора.
    /// </summary>
    public class AlbumAccessParameters
    {
        public bool HeadAlbum { get; set; }
        public bool ParticipantsReadonly { get; set; }
        public bool Private { get; set; }

        public static AlbumAccessParameters OwnerOnlyDefault() => new()
        {
            Private = true,
            ParticipantsReadonly = true
        };

        public static AlbumAccessParameters FromEvent(EventAlbumParameters? parameters) => parameters == null
            ? new AlbumAccessParameters()
            : new AlbumAccessParameters
            {
                HeadAlbum = parameters.HeadAlbum,
                ParticipantsReadonly = parameters.ParticipantsReadonly,
                Private = parameters.Private
            };

        public static AlbumAccessParameters FromAccount(AccountAlbumParameters? parameters) => parameters == null
            ? OwnerOnlyDefault()
            : new AlbumAccessParameters
            {
                HeadAlbum = parameters.HeadAlbum,
                ParticipantsReadonly = parameters.ParticipantsReadonly,
                Private = parameters.Private
            };
    }
}
