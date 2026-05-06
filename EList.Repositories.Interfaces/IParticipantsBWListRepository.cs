using EList.Common.Models;
using EList.Models.Participation;

namespace EList.Repositories.Interfaces
{
    public interface IParticipantsBWListRepository
    {
        Task AddToBlackListAsync(AddUsersToBWListRequest request);
        Task AddToWhiteListAsync(AddUsersToBWListRequest request);

        Task<bool> IsUserInBlackListAsync(Guid eventId, Guid accountId);
        Task<bool> IsUserInWhiteListAsync(Guid eventId, Guid accountId);

        Task DeleteFromBlackListAsync(Guid eventId, Guid accountId);
        Task DeleteFromWhiteListAsync(Guid eventId, Guid accountId);

        Task<PagedList<ParticipantBlackListItem>> GetEventBlackListAsync(Guid eventId, int? pageIndex, int? pageSize);
        Task<PagedList<ParticipantWhiteListItem>> GetEventWhiteListAsync(Guid eventId, int? pageIndex, int? pageSize);
    }
}
