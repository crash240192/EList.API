using EList.Common.Models;
using EList.Models.Events;

namespace EList.Validators.Interfaces
{
    /// <summary>
    /// Проверка доступности мероприятия зрителю (та же логика, что в GetEventAsync).
    /// </summary>
    public interface IEventAccessValidator
    {
        /// <summary>
        /// Можно ли зрителю просматривать мероприятие (private/WL/BL + возраст 18+).
        /// Организатор всегда имеет доступ.
        /// </summary>
        Task<CommandResult> AssertCanViewEventAsync(
            Guid eventId,
            Guid? viewerAccountId,
            bool adultConfirmed);

        /// <summary>
        /// То же, что <see cref="AssertCanViewEventAsync"/>, но по уже загруженному событию.
        /// </summary>
        Task<CommandResult> AssertCanViewEventAsync(
            Event eventItem,
            Guid? viewerAccountId,
            bool adultConfirmed,
            bool? isOrganizator = null);

        Task<bool> IsAccountEventOrganizatorAsync(Guid eventId, Guid accountId);
    }
}
