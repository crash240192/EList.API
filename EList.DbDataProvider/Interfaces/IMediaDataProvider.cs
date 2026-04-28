using EList.DbDataProvider.Models;

namespace EList.DbDataProvider.Interfaces
{
    public interface IMediaDataProvider
    {
        Task<Guid?> CreateAlbumAsync(AlbumRequest request);
        Task UpdateAlbumAsync(MediaAlbumDto item);

        Task AssingAlbumToAccountAsync(Guid accountId, Guid albumId);
        Task AssingAlbumToEventAsync(Guid eventId, Guid albumId);
        Task<MediaAlbumDto> GetAlbumAsync(Guid id);
        Task<List<MediaAlbumDto>> GetAccountAlbumsAsync(Guid accountId);
        Task<List<MediaAlbumDto>> GetEventAlbumsAsync(Guid eventId);

        Task SetNewAccountAvatarAsync(Guid accountId, Guid fileId);
        Task<List<Guid>?> GetAccountAvatarsAsync(Guid accountId);
        Task<Guid?> GetLastAccountAvatarAsync(Guid accountId);

        Task SetNewOrganizationAvatarAsync(Guid organizationId, Guid fileId);
        Task<List<Guid>?> GetOrganizationAvatarsAsync(Guid organizationId);
        Task<Guid?> GetLastOrganizationAvatarAsync(Guid organizationId);
    }
}
