using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;

namespace EList.DbDataProvider.DataProviders
{
    public class EventTicketStaffDataProvider : DataProviderBase, IEventTicketStaffDataProvider
    {
        public EventTicketStaffDataProvider(IDataConnectionProvider dataConnectionProvider)
            : base(dataConnectionProvider)
        {
        }

        public async Task<List<EventTicketStaffDto>> GetByEventIdAsync(Guid eventId)
        {
            return await _connection.EventTicketStaff
                .Where(i => i.EventId == eventId)
                .OrderBy(i => i.CreateDate)
                .ToListAsync();
        }

        public async Task<List<EventTicketStaffDto>> GetByAccountIdAsync(Guid accountId)
        {
            return await _connection.EventTicketStaff
                .Where(i => i.AccountId == accountId)
                .OrderByDescending(i => i.CreateDate)
                .ToListAsync();
        }

        public async Task<EventTicketStaffDto?> GetAsync(Guid eventId, Guid accountId)
        {
            return await _connection.EventTicketStaff
                .FirstOrDefaultAsync(i => i.EventId == eventId && i.AccountId == accountId);
        }

        public async Task<bool> CanAccountCheckInAsync(Guid eventId, Guid accountId)
        {
            return await _connection.EventTicketStaff
                .AnyAsync(i => i.EventId == eventId && i.AccountId == accountId && i.CanCheckIn);
        }

        public async Task<bool> CanAccountViewStatsAsync(Guid eventId, Guid accountId)
        {
            return await _connection.EventTicketStaff
                .AnyAsync(i => i.EventId == eventId && i.AccountId == accountId && i.CanViewStats);
        }

        public async Task ReplaceForEventAsync(Guid eventId, List<EventTicketStaffDto> staff)
        {
            await _connection.EventTicketStaff.DeleteAsync(i => i.EventId == eventId);
            if (staff == null || staff.Count == 0)
                return;

            await _connection.BulkCopyAsync(staff);
        }

        public async Task DeleteByEventAndAccountAsync(Guid eventId, Guid accountId)
        {
            await _connection.EventTicketStaff
                .DeleteAsync(i => i.EventId == eventId && i.AccountId == accountId);
        }
    }
}
