using EList.Common.Models;
using EList.Models.Invitations;

namespace EList.Services.Interfaces
{
    public interface IInvitationsService
    {
        Task<CommandResult> CreateAsync(CreateInvitationsRequest request);
        Task<CommandResult<PagedList<Invitation>>> GetUserInvitationsAsync();
        Task<CommandResult> AcceptAsync(Guid invitationId);
        Task<CommandResult> DeclineAsync(Guid invitationId);
        Task<CommandResult> CancelInvitationAsync(Guid invitationId);
        Task<CommandResult<PagedList<Invitation>>> SearchAsync(InvitationsSearchRequest request);
    }
}
