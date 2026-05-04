using EList.DbDataProvider.Models;

namespace EList.DbDataProvider.Interfaces
{
    public interface IParticipantsBWListDataProvider
    {
        Task<Guid> AddToBlackListAsync(Guid eventId, Guid accountId);
        Task<Guid> AddToWhiteListAsync(Guid eventId, Guid accountId);

        Task<bool> IsUserInBlackListAsync(Guid eventId, Guid accountId);
        Task<bool> IsUserInWhiteListAsync(Guid eventId, Guid accountId);

        Task DeleteFromBlackListAsync(Guid eventId, Guid accountId);
        Task DeleteFromWhiteListAsync(Guid eventId, Guid accountId);

        Task<ListResponse<ParticipantsBlackListItemDto>> GetEventBlackListAsync(Guid eventId, int? pageIndex, int? pageSize);
        Task<ListResponse<ParticipantsWhiteListItemDto>> GetEventWhiteListAsync(Guid eventId, int? pageIndex, int? pageSize);
    }
}
