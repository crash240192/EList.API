using EList.DbDataProvider.Models;

namespace EList.DbDataProvider.Interfaces
{
    public interface IEventTicketStaffDataProvider
    {
        Task<List<EventTicketStaffDto>> GetByEventIdAsync(Guid eventId);
        Task<List<EventTicketStaffDto>> GetByAccountIdAsync(Guid accountId);
        Task<EventTicketStaffDto?> GetAsync(Guid eventId, Guid accountId);
        Task<bool> CanAccountCheckInAsync(Guid eventId, Guid accountId);
        Task<bool> CanAccountViewStatsAsync(Guid eventId, Guid accountId);
        Task ReplaceForEventAsync(Guid eventId, List<EventTicketStaffDto> staff);
        Task DeleteByEventAndAccountAsync(Guid eventId, Guid accountId);
    }
}
