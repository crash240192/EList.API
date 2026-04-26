using EList.Common.Models;
using EList.Models.Participation;

namespace EList.Services.Interfaces
{
    public interface IParticipationsService
    {
        Task<CommandResult<Guid?>> ParticipateAsync(Guid eventId);
        Task<CommandResult> LeaveEventAsync(Guid eventId);
        Task<CommandResult<PagedList<Participant>>> GetEventParticipantsAsync(EventParticipantsSearchRequest request);
    }
}
