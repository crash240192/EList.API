using EList.DbDataProvider.Models;

namespace EList.DbDataProvider.Interfaces
{
    public interface IParticipationsDataProvider
    {
        Task<Guid> ParticipateAsync(Guid accountId, Guid eventId);
        Task LeaveEventAsync(Guid accountId, Guid eventId);
        Task<List<AccountDto>> GetEventParticipantsAsync(Guid eventId);
    }
}
