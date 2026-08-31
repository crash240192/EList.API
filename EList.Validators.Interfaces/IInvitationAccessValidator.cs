using EList.Common.Models;
using EList.Models.Events;
using EList.Models.Invitations;

namespace EList.Validators.Interfaces
{
    public interface IInvitationAccessValidator
    {
        /// <summary>
        /// Просмотр приглашения: пригласивший, приглашённый,
        /// организаторы мероприятия и участники организаций-организаторов.
        /// </summary>
        Task<CommandResult> AssertCanViewInvitationAsync(Invitation invitation, Guid? viewerAccountId);

        Task<CommandResult> AssertCanCreateInvitationsAsync(
            Event eventItem,
            Guid? viewerAccountId,
            Guid? inviterOrganizationId);

        Task<CommandResult> AssertCanAcceptOrDeclineAsync(Invitation invitation, Guid? viewerAccountId);

        Task<CommandResult> AssertCanCancelInvitationAsync(Invitation invitation, Guid? viewerAccountId);

        Task<bool> CanViewInvitationAsync(Invitation invitation, Guid? viewerAccountId);
    }
}
