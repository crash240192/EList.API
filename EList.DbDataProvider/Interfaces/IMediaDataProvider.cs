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
        Task<MediaAlbumDto> GetAlbumAsync(Guid id);
        Task<List<MediaAlbumDto>> GetAccountAlbumsAsync(Guid accountId);
        Task<List<MediaAlbumDto>> GetEventAlbumsAsync(Guid eventId);
        Task<ListResponse<EventAlbumsGroupDto>> GetEventsAlbumsAsync(Guid accountId, Guid? curAccountId, int? pageIndex = null, int? pageSize = null);
        Task<ListResponse<FileAlbumRelationDto>> GetAlbumFilesAsync(Guid albumId, int? pageIndex = null, int? pageSize = null);
        Task<FileAlbumRelationDto> GetFileAsync(Guid fileId, Guid albumId);
        Task<bool> CheckFileExistsAsync(List<Guid> fileIds);
        Task<List<Guid>> GetFilesNotExistsInAnotherAlbumsAsync(List<Guid> fileIds, Guid exceptAlbumId);
        Task<bool> SomeAlbumContainsThisFileAsync(Guid fileId);
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
    }
}
