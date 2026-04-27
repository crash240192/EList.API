using EList.Common.Models;
using EList.Models.Media;

namespace EList.Repositories.Interfaces
{
    public interface IMediaRepository
    {
        Task<Guid?> CreateAlbumAsync(EventAlbumRequest request);
        Task UpdateAlbumAsync(EventAlbumRequest request);

        Task AssingAlbumToAccountAsync(Guid accountId, Guid albumId);
        Task AssingAlbumToEventAsync(Guid eventId, Guid albumId);

        Task<MediaAlbum> GetAlbumAsync(Guid id);
        Task<List<MediaAlbum>> GetAccountAlbumsAsync(Guid accountId);
        Task<List<MediaAlbum>> GetEventAlbumsAsync(Guid eventId);

        Task SetNewAccountAvatarAsync(Guid accountId, Guid fileId);
        Task<List<Guid>?> GetAccountAvatarsAsync(Guid accountId);
        Task<Guid?> GetLastAccountAvatarAsync(Guid accountId);

        Task SetNewOrganizationAvatarAsync(Guid organizationId, Guid fileId);
        Task<List<Guid>?> GetOrganizationAvatarsAsync(Guid organizationId);
        Task<Guid?> GetLastOrganizationAvatarAsync(Guid organizationId);
    }
}
