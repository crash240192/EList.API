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
        AlbumAccessParameters ResolveParameters(MediaAlbum album);

        Task<CommandResult> AssertCanViewAlbumAsync(MediaAlbum album, Guid? viewerAccountId);

        Task<CommandResult> AssertCanModifyAlbumAsync(
            MediaAlbum album,
            Guid? viewerAccountId,
            AlbumAccessOperation operation);

        Task<List<MediaAlbum>> FilterViewableAlbumsAsync(
            IEnumerable<MediaAlbum> albums,
            Guid ownerAccountId,
            Guid? viewerAccountId);
    }
}
