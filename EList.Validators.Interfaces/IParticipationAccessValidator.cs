using EList.Common.Models;
using EList.Models.Events;

namespace EList.Validators.Interfaces
{
    public interface IParticipationAccessValidator
    {
        /// <summary>
        /// Просмотр списка участников — по доступности мероприятия.
        /// </summary>
        Task<CommandResult> AssertCanViewParticipantsAsync(
            Event eventItem,
            Guid? viewerAccountId,
            bool adultConfirmed);

        /// <summary>
        /// Чтение/изменение чёрного и белого списков — только организатор.
        /// </summary>
        Task<CommandResult> AssertCanManageBwListsAsync(Guid eventId, Guid? viewerAccountId);
    }
}
