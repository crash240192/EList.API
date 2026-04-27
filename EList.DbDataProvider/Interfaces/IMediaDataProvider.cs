using EList.DbDataProvider.Models;

namespace EList.DbDataProvider.Interfaces
{
    public interface IMediaDataProvider
    {
        Task<Guid?> CreateAlbumAsync(MediaAlbumDto item);
        Task UpdateAlbumAsync(MediaAlbumDto item);
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
