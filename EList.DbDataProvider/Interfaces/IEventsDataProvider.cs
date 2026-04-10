using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.SearchRequests;

namespace EList.DbDataProvider.Interfaces
{
    public interface IEventsDataProvider
    {
        Task<Guid> CreateEventAsync(EventDto item);
        Task<EventDto> GetEventAsync(Guid id);
        Task UpdateEventAsync(EventDto item);
        Task<(int, List<EventDto>)> SearchEventsAsync(EventsSearchRequest request);
    }
}
