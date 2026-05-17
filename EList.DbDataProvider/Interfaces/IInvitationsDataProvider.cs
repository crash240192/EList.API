using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.SearchRequests;

namespace EList.DbDataProvider.Interfaces
{
    public interface IInvitationsDataProvider
    {
        Task CreateInvitationsAsync(InvitationDto invitation);
        Task CreateInvitationsAsync(List<InvitationDto> invitations);
        Task<InvitationDto> GetInvitationAsync(Guid id);
        Task<InvitationDto> GetInvitationAsync(Guid invitedAccountId, Guid eventId);
        Task<ListResponse<InvitationDto>> SearchInvitationsAsync(InvitationsSearchRequest request);
        Task DeleteInvitationAsync(Guid id);
        Task CancelInvitationsAsync(Guid eventId);
        Task DeleteInvitationAsync(Guid eventId, Guid accountId);
    }
}
