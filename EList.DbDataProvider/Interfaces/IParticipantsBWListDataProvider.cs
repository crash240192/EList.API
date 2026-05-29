using EList.DbDataProvider.DataProviders;
using EList.DbDataProvider.Models;

namespace EList.DbDataProvider.Interfaces
{
    public interface IParticipantsBWListDataProvider
    {
        Task AddToBlackListAsync(Guid eventId, List<Guid> accountIds);
        Task AddToWhiteListAsync(Guid eventId, List<Guid> accountIds);

        Task<bool> IsUserInBlackListAsync(Guid eventId, Guid accountId);
        Task<bool> IsUserInWhiteListAsync(Guid eventId, Guid accountId);
        Task<bool> IsWhiteListEmptyAsync(Guid eventId);

        Task<List<Guid>> FilterUsersNotInWhiteListAsync(Guid eventId, List<Guid> accountIds);
        Task<List<Guid>> FilterUsersNotInBlackListAsync(Guid eventId, List<Guid> accountIds);

        Task DeleteFromBlackListAsync(Guid eventId, Guid accountId);
        Task DeleteFromWhiteListAsync(Guid eventId, Guid accountId);

        Task<ListResponse<ParticipantsBlackListItemDto>> GetEventBlackListAsync(Guid eventId, int? pageIndex, int? pageSize);
        Task<ListResponse<ParticipantsWhiteListItemDto>> GetEventWhiteListAsync(Guid eventId, int? pageIndex, int? pageSize);

        Task<List<Guid>> GetEventBlackListShortAsync(Guid eventId);
        Task<List<Guid>> GetEventWhiteListShortAsync(Guid eventId);

        Task<int> BlackListPersonsCountAsync(Guid eventId);
        Task<int> WhiteListPersonsCountAsync(Guid eventId);
    }
}
