using EList.Common.Models;
using EList.Models.Participation;
using EList.Models.Subscriptions;

namespace EList.Repositories.Interfaces
{
    public interface IParticipationsRepository
    {
        Task<Guid> ParticipateAsync(Guid accountId, Guid eventId);
        Task LeaveEventAsync(Guid accountId, Guid eventId);
        Task<PagedList<Participant>> GetEventParticipantsAsync(EventParticipantsSearchRequest request);
        Task<int> GetParticipantsCountAsync(Guid eventId);
    }
}
