using EList.Models.Events;

namespace EList.Repositories.Interfaces
{
    public interface IEventTicketStaffRepository
    {
        Task<List<EventTicketStaff>> GetByEventIdAsync(Guid eventId);
        Task<List<EventTicketStaff>> GetByAccountIdAsync(Guid accountId);
        Task<EventTicketStaff?> GetAsync(Guid eventId, Guid accountId);
        Task<bool> CanAccountCheckInAsync(Guid eventId, Guid accountId);
        Task<bool> CanAccountViewStatsAsync(Guid eventId, Guid accountId);
        Task ReplaceForEventAsync(Guid eventId, List<EventTicketStaff> staff);
        Task DeleteByEventAndAccountAsync(Guid eventId, Guid accountId);
    }
}
