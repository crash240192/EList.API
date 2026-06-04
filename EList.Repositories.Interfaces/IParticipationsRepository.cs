using EList.Common.Models;
using EList.Models.Participation;

namespace EList.Repositories.Interfaces
{
    public interface IParticipationsRepository
    {
        Task<Guid> ParticipateAsync(Guid accountId, Guid eventId);
        Task LeaveEventAsync(Guid accountId, Guid eventId);
        Task DropParticipationsAsync(Guid eventId, List<Guid> accountIds);
        Task DropAllParticipationsExceptThisUsersAsync(Guid eventId, List<Guid> accountIds);
        Task<PagedList<Participant>> GetEventParticipantsAsync(EventParticipantsSearchRequest request);
        Task<List<Guid>> GetEventParticipantIdsAsync(Guid eventId);
        Task<bool> IsUserParticipatedAsync(Guid accountId, Guid eventId);
        Task<int> GetParticipantsCountAsync(Guid eventId);
    }
}
