using EList.Common.Models;
using EList.Models.Events;

namespace EList.Repositories.Interfaces
{
    public interface IEventsRepository
    {
        Task<Guid> CreateEventAsync(EventRequest request);
        Task<Event> GetEventAsync(Guid id);
        Task UpdateEventAsync(Guid id, EventRequest request);
        Task SetEventCoverImageAsync(Guid id, Guid? imageId);
        Task<PagedList<Event>> SearchEventsAsync(EventsSearchRequest request, Guid? curAccountId);
        Task<PagedList<EventShort>> SearchEventsShortAsync(EventsSearchRequest request, Guid? curAccountId);
        Task CancelEventAsync(Guid eventId);
    }
}
