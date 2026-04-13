using EList.Models.Accounts;
using EList.Models.Participation;
using EList.Models.Person;

namespace EList.Repositories.Interfaces
{
    public interface IParticipationsRepository
    {
        Task<Guid> ParticipateAsync(Guid accountId, Guid eventId);
        Task LeaveEventAsync(Guid accountId, Guid eventId);
        Task<List<Participant>> GetEventParticipantsAsync(Guid eventId);
    }
}
