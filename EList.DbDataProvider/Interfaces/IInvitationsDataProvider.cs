using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.SearchRequests;

namespace EList.DbDataProvider.Interfaces
{
    public interface IInvitationsDataProvider
    {
        Task CreateInvitationsAsync(InvitationDto invitation);
        Task<InvitationDto> GetInvitationAsync(Guid id);
        Task<(int, List<InvitationDto>)> SearchInvitationsAsync(InvitationsSearchRequest request);
        Task DeleteInvitationAsync(Guid id);
        Task DeleteInvitationAsync(Guid eventId, Guid accountId);
    }
}
