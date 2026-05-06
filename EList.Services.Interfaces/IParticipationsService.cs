using EList.Common.Models;
using EList.Models.Participation;

namespace EList.Services.Interfaces
{
    public interface IParticipationsService
    {
        Task<CommandResult<Guid?>> ParticipateAsync(Guid eventId);
        Task<CommandResult> LeaveEventAsync(Guid eventId);
        Task<CommandResult<PagedList<Participant>>> GetEventParticipantsAsync(EventParticipantsSearchRequest request);

        Task<CommandResult<PagedList<ParticipantBlackListItem>>> GetEventBlackListAsync(Guid eventId, int? pageIndex, int? pageSize);
        Task<CommandResult<PagedList<ParticipantWhiteListItem>>> GetEventWhiteListAsync(Guid eventId, int? pageIndex, int? pageSize);
        Task<CommandResult> AddToBlackListAsync(AddUsersToBWListRequest request);
        Task<CommandResult> AddToWhiteListAsync(AddUsersToBWListRequest request);
        Task<CommandResult> DeleteFromBlackListAsync(Guid eventId, Guid accountId);
        Task<CommandResult> DeleteFromWhiteListAsync(Guid eventId, Guid accountId);
    }
}
