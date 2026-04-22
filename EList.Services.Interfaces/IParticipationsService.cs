using EList.Common.Models;
using EList.Models.Participation;

namespace EList.Services.Interfaces
{
    public interface IParticipationsService
    {
        Task<CommandResult<Guid?>> ParticipateAsync(Guid id);
        Task<CommandResult> LeaveEventAsync(Guid id);
        Task<CommandResult<PagedList<Participant>>> GetEventParticipantsAsync(EventParticipantsSearchRequest request);
    }
}
