using EList.Common.Models;
using EList.Models.EventsRating;

namespace EList.Services.Interfaces
{
    public interface IEventsRatingService
    {
        Task<CommandResult> VoteAsync(EventsRatingItem request);
        Task<CommandResult<EventRating>> GetEventRatingAsync(Guid eventId);
        Task<CommandResult<int?>> GetOrganizatorRatingAsync(Guid accountId);
    }
}
