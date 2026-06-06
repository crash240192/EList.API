using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.SearchRequests;

namespace EList.DbDataProvider.Interfaces
{
    public interface IInvitationsDataProvider
    {
        Task CreateInvitationsAsync(InvitationDto invitation);
        Task CreateInvitationsAsync(List<InvitationDto> invitations);
        Task<InvitationDto> GetInvitationAsync(Guid id);
        Task<InvitationDto> GetFullInvitationAsync(Guid id);
        Task<int> GetNotViewedInvitationsCountAsync(Guid accountId);
        Task ViewInvitationAsync(Guid invitationId);
        Task ViewAllInvitationsAsync(Guid accountId);
        Task<List<InvitationDto>?> GetAllEventInvitationsAsync(Guid eventId);
        Task<InvitationDto> GetInvitationAsync(Guid invitedAccountId, Guid eventId);
        Task<bool> IsUserInvitatedAsync(Guid accountId, Guid eventId);
        Task<ListResponse<InvitationDto>> SearchInvitationsAsync(InvitationsSearchRequest request);
        Task DeleteInvitationAsync(Guid id);
        Task CancelInvitationsAsync(Guid eventId);
        Task CancelAllInvitationsExceptThisUsersAsync(Guid eventId, List<Guid> accountIds);
        Task CancelAllInvitationsExceptWhiteListAsync(Guid eventId);
        Task DeleteInvitationAsync(Guid eventId, Guid accountId);
        Task DeleteInvitationAsync(Guid eventId, List<Guid> accountIds);
    }
}
