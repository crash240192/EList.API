using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Events;
using EList.Models.Invitations;
using EList.Repositories.Interfaces;
using EList.Validators.Interfaces;

namespace EList.Validators.Impl
{
    public class InvitationAccessValidator : IInvitationAccessValidator
    {
        private readonly IEventAccessValidator _eventAccessValidator;
        private readonly IEventOrganizatorsRepository _eventOrganizatorsRepository;
        private readonly IOrganizationsRepository _organizationsRepository;

        public InvitationAccessValidator(
            IEventAccessValidator eventAccessValidator,
            IEventOrganizatorsRepository eventOrganizatorsRepository,
            IOrganizationsRepository organizationsRepository)
        {
            _eventAccessValidator = eventAccessValidator;
            _eventOrganizatorsRepository = eventOrganizatorsRepository;
            _organizationsRepository = organizationsRepository;
        }

        public async Task<CommandResult> AssertCanViewInvitationAsync(Invitation invitation, Guid? viewerAccountId)
        {
            if (await CanViewInvitationAsync(invitation, viewerAccountId))
                return CommandResult.OK;

            return CommandResult.Fail(ErrorCode.AccessError, "Нет доступа к этому приглашению");
        }

        public async Task<bool> CanViewInvitationAsync(Invitation invitation, Guid? viewerAccountId)
        {
            if (viewerAccountId == null)
                return false;

            if (invitation.InviterAccountId == viewerAccountId
                || invitation.InvitedAccountId == viewerAccountId)
                return true;

            // Организаторы мероприятия и активные участники организаций-организаторов.
            if (await _eventAccessValidator.IsAccountEventOrganizatorAsync(invitation.EventId, viewerAccountId.Value))
                return true;

            if (invitation.InviterOrganizationId != null
                && await _organizationsRepository.IsActiveMemberAsync(
                    invitation.InviterOrganizationId.Value, viewerAccountId.Value))
                return true;

            return false;
        }

        public async Task<CommandResult> AssertCanCreateInvitationsAsync(
            Event eventItem,
            Guid? viewerAccountId,
            Guid? inviterOrganizationId)
        {
            if (viewerAccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var isOrganizator = await _eventAccessValidator.IsAccountEventOrganizatorAsync(
                eventItem.Id, viewerAccountId.Value);

            if (inviterOrganizationId != null)
            {
                var organizators = await _eventOrganizatorsRepository.GetByEventIdAsync(eventItem.Id);
                var orgIsEventOrganizator = organizators?.Any(i => i.OrganizationId == inviterOrganizationId) ?? false;

                if (!isOrganizator || !orgIsEventOrganizator)
                    return CommandResult.Fail(
                        ErrorCode.AccessError,
                        "Организация не является организатором мероприятия или у вас нет прав");

                if (!await _organizationsRepository.IsActiveMemberAsync(inviterOrganizationId.Value, viewerAccountId.Value))
                    return CommandResult.Fail(ErrorCode.AccessError, "Вы не являетесь участником указанной организации");
            }

            var allowUsersToInvite = eventItem.Parameters?.AllowUsersToInvite == true;
            if (!allowUsersToInvite && !isOrganizator)
                return CommandResult.Fail(
                    ErrorCode.AccessError,
                    "Приглашения на текущее событие запрещены администратором");

            return CommandResult.OK;
        }

        public Task<CommandResult> AssertCanAcceptOrDeclineAsync(Invitation invitation, Guid? viewerAccountId)
        {
            if (viewerAccountId == null)
                return Task.FromResult(CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться"));

            if (invitation.InvitedAccountId != viewerAccountId)
                return Task.FromResult(CommandResult.Fail(
                    ErrorCode.AccessError,
                    "У текущего пользователя нет доступа для взаимодействия с этим приглашением"));

            return Task.FromResult(CommandResult.OK);
        }

        public async Task<CommandResult> AssertCanCancelInvitationAsync(Invitation invitation, Guid? viewerAccountId)
        {
            if (viewerAccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            if (invitation.InviterAccountId == viewerAccountId)
                return CommandResult.OK;

            if (invitation.InviterOrganizationId != null
                && await _organizationsRepository.IsOwnerOrManagerAsync(
                    invitation.InviterOrganizationId.Value, viewerAccountId.Value))
                return CommandResult.OK;

            if (await _eventAccessValidator.IsAccountEventOrganizatorAsync(invitation.EventId, viewerAccountId.Value))
                return CommandResult.OK;

            return CommandResult.Fail(ErrorCode.AccessError, "У текущего пользователя нет доступа отмены этого приглашения");
        }
    }
}
