using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;
using EList.DbDataProvider.Models.SearchRequests;

namespace EList.DbDataProvider.Interfaces
{
    public interface IParticipationsDataProvider
    {
        Task<Guid> ParticipateAsync(Guid accountId, Guid eventId);
        Task LeaveEventAsync(Guid accountId, Guid eventId);
        Task<(int, List<AccountDto>)> GetEventParticipantsAsync(EventParticipantsSearchRequest request);
    }
}
