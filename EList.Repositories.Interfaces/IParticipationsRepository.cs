using EList.Models.Person;

namespace EList.Repositories.Interfaces
{
    public interface IParticipationsRepository
    {
        Task<Guid> ParticipateAsync(Guid accountId, Guid eventId);
        Task LeaveEventAsync(Guid accountId, Guid eventId);
        Task<List<PersonInfo>> GetEventParticipantsAsync(Guid eventId);
    }
}
