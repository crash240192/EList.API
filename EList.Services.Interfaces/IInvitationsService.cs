using EList.Common.Models;
using EList.Models.Invitations;

namespace EList.Services.Interfaces
{
    public interface IInvitationsService
    {
        Task<CommandResult> CreateAsync(CreateInvitationsRequest request);
        Task<CommandResult<CreateInvitationsToAccountResult>> CreateToAccountAsync(CreateInvitationsToAccountRequest request);

        /// <summary>
        /// Единая проверка: можно ли <paramref name="inviterAccountId"/> пригласить
        /// <paramref name="inviteeAccountId"/> на мероприятие.
        /// </summary>
        Task<CommandResult<InviteToEventEligibility>> AssertCanInviteToEventAsync(
            Guid inviterAccountId,
            Guid inviteeAccountId,
            Guid eventId,
            Guid? inviterOrganizationId = null);

        Task<CommandResult<List<InviteToEventEligibility>>> CanInviteToEventByEventAsync(
            CanInviteToEventByEventRequest request);

        Task<CommandResult<List<InviteToEventEligibility>>> CanInviteToEventByAccountAsync(
            CanInviteToEventByAccountRequest request);

        Task<CommandResult<PagedList<Invitation>>> GetUserInvitationsAsync(int pageIndex = 0, int pageSize = 20);
        Task<CommandResult> AcceptAsync(Guid invitationId);
        Task<CommandResult> DeclineAsync(Guid invitationId);
        Task<CommandResult> CancelInvitationAsync(Guid invitationId);
        Task<CommandResult<PagedList<Invitation>>> SearchAsync(InvitationsSearchRequest request);
        Task<CommandResult<int>> GetNotViewedInvitationsCountAsync();
        Task<CommandResult> ViewInvitationAsync(Guid invitationId);
        Task<CommandResult> ViewAllInvitationsAsync();
    }
}
