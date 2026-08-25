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
        Task<PagedList<Event>> SearchEventsAsync(EventsSearchRequest request, Guid? curAccountId, bool adultConfirmed);
        Task<PagedList<EventShort>> SearchEventsShortAsync(EventsSearchRequest request, Guid? curAccountId, bool adultConfirmed);
        Task CancelEventAsync(Guid eventId, Guid? cancelledByAccountId = null, string? cancelSource = null, Guid? cancelReportId = null);
        Task RestoreEventAsync(Guid eventId);

        Task<int> CountActiveEventsByAccountOrganizatorAsync(Guid accountId);
        Task<int> CountActiveEventsByOrganizationOrganizatorAsync(Guid organizationId);
        Task<int> CountEventsCreatedByAccountSinceAsync(Guid accountId, DateTimeOffset since);
        Task<int> CountEventsCreatedByOrganizationSinceAsync(Guid organizationId, DateTimeOffset since);
        Task<int> CountEventsNearLocationSinceAsync(
            Guid? accountId,
            Guid? organizationId,
            double latitude,
            double longitude,
            double radiusMeters,
            DateTimeOffset since);
    }
}
