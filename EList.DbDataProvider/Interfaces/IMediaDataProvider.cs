using EList.DbDataProvider.Models;

namespace EList.DbDataProvider.Interfaces
{
    public interface IMediaDataProvider
    {
        Task<Guid?> CreateAlbumAsync(AlbumRequest request);
        Task UpdateAlbumAsync(AlbumRequest item);

        Task AssignAlbumToAccountAsync(Guid accountId, Guid albumId);
        Task AssignAlbumToEventAsync(Guid eventId, Guid albumId);
        Task AddFilesToAlbumAsync(Guid albumId, List<Guid> fileIds);
        Task RemoveFilesFromAlbumAsync(Guid albumId, List<Guid> fileIds);
        Task<int> CountAlbumFilesAsync(Guid albumId);
        /// <summary>Число видимых файлов по списку альбомов одним запросом.</summary>
        Task<Dictionary<Guid, int>> CountAlbumFilesAsync(IReadOnlyList<Guid> albumIds);
        /// <summary>Первые N видимых fileId по каждому альбому (ORDER BY create_date).</summary>
        Task<Dictionary<Guid, List<Guid>>> GetFirstAlbumFileIdsAsync(IReadOnlyList<Guid> albumIds, int limit);
        Task<Guid?> FindEventSystemAlbumIdAsync(Guid eventId, short systemKind);
        Task RegisterEventSystemAlbumAsync(Guid eventId, short systemKind, Guid albumId);
        Task<MediaAlbumDto> GetAlbumAsync(Guid id);
        Task<List<MediaAlbumDto>> GetAccountAlbumsAsync(Guid accountId);
        Task<List<MediaAlbumDto>> GetEventAlbumsAsync(Guid eventId);
        /// <param name="sort">date | latestPhotos | name — см. <see cref="EList.Models.Media.EventAlbumsSort"/>.</param>
        Task<ListResponse<EventAlbumsGroupDto>> GetEventsAlbumsAsync(
            Guid accountId, Guid? curAccountId, int? pageIndex = null, int? pageSize = null, string? sort = null);
        Task<ListResponse<FileAlbumRelationDto>> GetAlbumFilesAsync(Guid albumId, int? pageIndex = null, int? pageSize = null);
        Task<FileAlbumRelationDto> GetFileAsync(Guid fileId, Guid albumId);
        Task<bool> CheckFileExistsAsync(List<Guid> fileIds);
        Task<List<Guid>> GetFilesNotExistsInAnotherAlbumsAsync(List<Guid> fileIds, Guid exceptAlbumId);
        Task<bool> SomeAlbumContainsThisFileAsync(Guid fileId);
        /// <summary>
        /// Returns file ids from the input that have no remaining refs
        /// (albums, messages, avatars, covers, bug reports, content reports).
        /// </summary>
        Task<List<Guid>> FilterUnreferencedFileIdsAsync(IReadOnlyList<Guid> fileIds);
        Task DeleteFilesAsync(List<Guid> fileIds);
        Task DeleteAlbumAsync(Guid albumId);

        Task SetNewAccountAvatarAsync(Guid accountId, Guid fileId);
        Task<List<Guid>?> GetAccountAvatarsAsync(Guid accountId);
        Task<Guid?> GetLastAccountAvatarAsync(Guid accountId);
        Task<AccountAvatarDto> GetAvatarAsync(Guid fileId);
        Task DeleteAvatarAsync(Guid fileId);

        Task SetNewOrganizationAvatarAsync(Guid organizationId, Guid fileId);
        Task<List<Guid>?> GetOrganizationAvatarsAsync(Guid organizationId);
        Task<Guid?> GetLastOrganizationAvatarAsync(Guid organizationId);
        Task DeleteOrganizationAvatarAsync(Guid fileId);

        Task UpsertAlbumViewAsync(Guid accountId, Guid albumId, DateTimeOffset viewedAt);
        Task<DateTimeOffset?> GetAlbumLastViewedAtAsync(Guid accountId, Guid albumId);
        Task<Dictionary<Guid, DateTimeOffset>> GetAlbumLastViewedAtAsync(Guid accountId, IReadOnlyList<Guid> albumIds);
        Task<DateTimeOffset?> GetAlbumLastContentAtAsync(Guid albumId);
        Task<Dictionary<Guid, DateTimeOffset>> GetAlbumLastContentAtAsync(IReadOnlyList<Guid> albumIds);
    }
}
