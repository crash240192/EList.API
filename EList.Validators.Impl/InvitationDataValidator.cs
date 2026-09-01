using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Invitations;
using EList.Validators.Interfaces;

namespace EList.Validators.Impl
{
    public class InvitationDataValidator : IInvitationDataValidator
    {
        public const int MaxInviteesPerRequest = 500;

        public CommandResult ValidateCreateRequest(CreateInvitationsRequest? request)
        {
            if (request == null)
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Запрос на создание приглашений не указан");

            if (request.EventId == Guid.Empty)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Не указан идентификатор мероприятия");

            if (request.InviterOrganizationId.HasValue && request.InviterOrganizationId.Value == Guid.Empty)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Некорректный идентификатор организации-пригласителя");

            return ValidationCommon.ValidateGuidList(
                request.AccountIds,
                "Список приглашаемых",
                MaxInviteesPerRequest,
                allowEmpty: false,
                requireAtLeastOne: true);
        }
    }
}
