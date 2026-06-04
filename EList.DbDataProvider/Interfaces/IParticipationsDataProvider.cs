using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.SearchRequests;

namespace EList.DbDataProvider.Interfaces
{
    public interface IParticipationsDataProvider
    {
        Task<Guid> ParticipateAsync(Guid accountId, Guid eventId);
        Task LeaveEventAsync(Guid accountId, Guid eventId);
        Task DropParticipationsAsync(Guid eventId, List<Guid> accountIds);
        Task DropAllParticipationsExceptThisUsersAsync(Guid eventId, List<Guid> accountIds);
        Task<bool> IsUserParticipatedAsync(Guid accountId, Guid eventId);
        Task<ListResponse<AccountDto>> GetEventParticipantsAsync(EventParticipantsSearchRequest request);
        Task<List<Guid>> GetEventParticipantIdsAsync(Guid eventId);
        Task<int> GetParticipantsCountAsync(Guid eventId);
    }
}
