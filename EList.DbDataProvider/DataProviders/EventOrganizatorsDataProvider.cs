using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;

namespace EList.DbDataProvider.DataProviders
{
    public class EventOrganizatorsDataProvider : DataProviderBase, IEventOrganizatorsDataProvider
    {
        public EventOrganizatorsDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }

        public async Task<Guid> CreateAsync(EventOrganizatorDto item)
        {
            var result = (Guid)await _connection.InsertWithIdentityAsync(item);
            return result;
        }

        public async Task UpdateAsync(EventOrganizatorDto request)
        {
            await _connection.Organizators.Where(i => i.Id == request.Id)
                .Set(i => i.AccountId, request.AccountId)
                .Set(i => i.EventId, request.EventId)
                .Set(i => i.OrganizationId, request.OrganizationId)
                .UpdateAsync();
        }

        public async Task<EventOrganizatorDto> GetByIdAsync(Guid id)
        {
            var result = await _connection.Organizators.FirstOrDefaultAsync(i => i.Id == id);
            return result;
        }

        public async Task<List<EventOrganizatorDto>> GetByEventIdAsync(Guid eventId)
        {
            var events = await _connection.Organizators
                .LoadWith(i => i.Account)
                .ThenLoad(i => i.PersonInfo)
                .Where(i => i.EventId == eventId).ToListAsync();
            return events;
        }

        public async Task AssignAsync(Guid eventId, List<Guid> accountIds)
        {
            if (accountIds?.Any() ?? false)
            {
                var organizators = await _connection.Organizators
                    .Where(i => i.EventId == eventId).ToListAsync();

                var organizatorItems = accountIds?.Where(i => !organizators.Any(o => o.AccountId == i))?.Select(i => new EventOrganizatorDto
                {
                    AccountId = i,
                    EventId = eventId
                })?.ToList();

                var res = await _connection.BulkCopyAsync(organizatorItems);
            }
        }
    }
}