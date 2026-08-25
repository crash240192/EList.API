using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.SearchRequests;

namespace EList.DbDataProvider.Interfaces
{
    public interface IEventsDataProvider
    {
        Task<Guid> CreateEventAsync(EventDto item);
        Task<EventDto> GetEventAsync(Guid id);
        Task UpdateEventAsync(EventDto item);
        Task SetEventCoverImageAsync(Guid eventId, Guid? imageId);
        Task<ListResponse<EventDto>> SearchEventsAsync(EventsSearchRequest request, Guid? curAccountId = null, bool adultConfirmed = false);
        Task CancelEventAsync(Guid eventId, Guid? cancelledByAccountId, string? cancelSource, Guid? cancelReportId);
        Task RestoreEventAsync(Guid eventId);

        /// <summary>Active upcoming/ongoing events where account is a direct organizator.</summary>
        Task<int> CountActiveEventsByAccountOrganizatorAsync(Guid accountId);

        /// <summary>Active upcoming/ongoing events where organization is an organizator.</summary>
        Task<int> CountActiveEventsByOrganizationOrganizatorAsync(Guid organizationId);

        /// <summary>Events created since timestamp where account is a direct organizator.</summary>
        Task<int> CountEventsCreatedByAccountSinceAsync(Guid accountId, DateTimeOffset since);

        /// <summary>Events created since timestamp where organization is an organizator.</summary>
        Task<int> CountEventsCreatedByOrganizationSinceAsync(Guid organizationId, DateTimeOffset since);

        /// <summary>
        /// Events by organizator near lat/lng (bbox approx) created since timestamp.
        /// Pass either accountId or organizationId.
        /// </summary>
        Task<int> CountEventsNearLocationSinceAsync(
            Guid? accountId,
            Guid? organizationId,
            double latitude,
            double longitude,
            double radiusMeters,
            DateTimeOffset since);
    }
}
