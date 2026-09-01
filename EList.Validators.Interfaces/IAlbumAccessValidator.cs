using EList.Common.Models;
using EList.Models.Media;

namespace EList.Validators.Interfaces
{
    public enum AlbumAccessOperation
    {
        View,
        ModifyMetadata,
        AddFiles,
        Assign,
        Delete
    }

    public interface IAlbumAccessValidator
    {
        /// <summary>
        /// Параметры доступа: при наличии EventId — только event_album_parameters;
        /// иначе — account_album_parameters (или owner-only по умолчанию).
        /// </summary>
        AlbumAccessParameters ResolveParameters(MediaAlbum album);

        Task<CommandResult> AssertCanViewAlbumAsync(
            MediaAlbum album,
            Guid? viewerAccountId,
            bool adultConfirmed);

        Task<CommandResult> AssertCanModifyAlbumAsync(
            MediaAlbum album,
            Guid? viewerAccountId,
            bool adultConfirmed,
            AlbumAccessOperation operation);

        Task<List<MediaAlbum>> FilterViewableAlbumsAsync(
            IEnumerable<MediaAlbum> albums,
            Guid? viewerAccountId,
            bool adultConfirmed);
    }
}
