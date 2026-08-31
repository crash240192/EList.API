using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Events;
using EList.Validators.Interfaces;

namespace EList.Validators.Impl
{
    public class ParticipationAccessValidator : IParticipationAccessValidator
    {
        private readonly IEventAccessValidator _eventAccessValidator;

        public ParticipationAccessValidator(IEventAccessValidator eventAccessValidator)
        {
            _eventAccessValidator = eventAccessValidator;
        }

        public async Task<CommandResult> AssertCanViewParticipantsAsync(
            Event eventItem,
            Guid? viewerAccountId,
            bool adultConfirmed)
        {
            return await _eventAccessValidator.AssertCanViewEventAsync(
                eventItem, viewerAccountId, adultConfirmed);
        }

        public async Task<CommandResult> AssertCanManageBwListsAsync(Guid eventId, Guid? viewerAccountId)
        {
            if (viewerAccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            if (!await _eventAccessValidator.IsAccountEventOrganizatorAsync(eventId, viewerAccountId.Value))
                return CommandResult.Fail(ErrorCode.AccessError, "Пользователь не является организатором события");

            return CommandResult.OK;
        }
    }
}
