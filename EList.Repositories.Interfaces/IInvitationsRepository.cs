using EList.Common.Models;
using EList.Models.Invitations;

namespace EList.Repositories.Interfaces
{
    public interface IInvitationsRepository
    {
        Task CreateInvitationsAsync(CreateInvitationsRequest request, Guid inviterAccountId);
        Task<PagedList<Invitation>> SearchInvitationsAsync(Models.Invitations.InvitationsSearchRequest request);
        Task DeleteInvitationAsync(Guid id);
        Task CancelAllInvitationsAsync(Guid eventId);
        Task CancelAllInvitationsExceptThisUsersAsync(Guid eventId, List<Guid> invitedAccountIds);
        Task DeleteInvitationAsync(Guid eventId, Guid accountId);
        Task DeleteInvitationAsync(Guid eventId, List<Guid> accountIds);
        Task <Invitation> GetInvitationAsync(Guid invitationId);
        Task<List<Invitation>?> GetAllEventInvitationsAsync(Guid eventId);
        Task<Invitation> GetInvitationAsync(Guid invitedAccountId, Guid eventId);
        Task<bool> IsUserInvitatedAsync(Guid accountId, Guid eventId);
    }
}
