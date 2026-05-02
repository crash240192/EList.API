using EList.Common.Models;
using EList.Models.Enums;
using EList.Models.EventsRating;

namespace EList.Services.Interfaces
{
    public interface IEventsRatingService
    {
        Task<CommandResult<Guid>> VoteAsync(EventsRatingItem request);
        Task<CommandResult<EventRating>> GetEventRatingAsync(Guid eventId, EventRatingType eventRatingType, int? pageIndex, int? pageSize);
        Task<CommandResult<int?>> GetOrganizatorRatingAsync(Guid accountId);
    }
}
