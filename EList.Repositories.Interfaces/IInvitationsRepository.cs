using EList.Common.Models;
using EList.Models.Invitations;

namespace EList.Repositories.Interfaces
{
    public interface IInvitationsRepository
    {
        Task CreateInvitationsAsync(CreateInvitationsRequest request, Guid inviterAccountId);
        Task<PagedList<Invitation>> SearchInvitationsAsync(Models.Invitations.InvitationsSearchRequest request);
        Task DeleteInvitationAsync(Guid id);
        Task DeleteInvitationAsync(Guid eventId, Guid accountId);
        Task <Invitation> GetInvitationAsync(Guid invitationId);
    }
}
