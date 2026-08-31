using EList.Common.Models;
using EList.Models.Media;

namespace EList.Services.Interfaces
{
    public interface IMediaService
    {
        Task<CommandResult<Guid?>> CreateAlbumAsync(EventAlbumRequest request);
        Task<CommandResult> UpdateAlbumAsync(EventAlbumRequest request);

        Task<CommandResult> AssignAlbumToEventAsync(Guid eventId, Guid albumId);
        Task<CommandResult> AssignAlbumToAccountAsync(Guid accountId, Guid albumId);

        Task<CommandResult<MediaAlbum>> GetAlbumAsync(Guid id);
        Task<CommandResult<List<MediaAlbum>>> GetAccountAlbumsAsync(Guid accountId);
        Task<CommandResult<List<MediaAlbum>>> GetEventAlbumsAsync(Guid eventId);
        Task<CommandResult<PagedList<EventAlbumsContainer>>> GetEventsAlbumsAsync(Guid accountId, int? pageIndex = null, int? pageSize = null);
        Task<CommandResult<PagedList<AlbumFile>>> GetAlbumFilesAsync(Guid albumId, int? pageIndex = null, int? pageSize = null);
        //Task<CommandResult> SetEventAlbumParametersAsync(Guid token, EventAlbumParameters request);
        Task<CommandResult> AddFilesToAlbumAsync(AddFilesRequest request);
        Task<CommandResult> DeleteAlbumAsync(Guid albumId);
        Task<CommandResult> DeleteFilesAsync(List<Guid> fileId, Guid albumId);

        Task<CommandResult> SetNewAccountAvatarAsync(Guid fileId);
        Task<CommandResult<List<Guid>?>> GetCurAccountAvatarsAsync();
        Task<CommandResult<List<Guid>?>> GetAccountAvatarsAsync(Guid accountId);
        Task<CommandResult<Guid?>> GetCurAccountAvatarAsync();
        Task<CommandResult<Guid?>> GetAccountAvatarAsync(Guid accountId);
        Task<CommandResult> DeleteAvatarAsync(Guid fileId);

        Task<CommandResult> SetNewOrganizationAvatarAsync(Guid organizationId, Guid fileId);
        Task<CommandResult<List<Guid>?>> GetOrganizationAvatarsAsync(Guid organizationId);
        Task<CommandResult<Guid?>> GetOrganizationAvatarAsync(Guid organizationId);

    }
}
