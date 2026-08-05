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
            var organizators = await _connection.Organizators
                .LoadWith(i => i.Account)
                .ThenLoad(i => i.PersonInfo)
                .LoadWith(i => i.Account)
                .ThenLoad(i => i.Avatars)
                .LoadWith(i => i.Organization)
                .Where(i => i.EventId == eventId).ToListAsync();
            return organizators;
        }

        public async Task<List<Guid>> GetOrganizatorIdsByEventIdAsync(Guid eventId)
        {
            var organizatorIds = await _connection.Organizators
                .Where(i => i.EventId == eventId)
                .Where(i => i.AccountId != null)
                .Select(i => i.AccountId.Value)
                .ToListAsync();
            return organizatorIds;
        }

        public async Task<bool> IsAccountEventOrganizatorAsync(Guid eventId, Guid accountId)
        {
            var isDirectOrganizator = await _connection.Organizators
                .AnyAsync(i => i.EventId == eventId && i.AccountId == accountId);
            if (isDirectOrganizator)
                return true;

            var organizationIds = await _connection.Organizators
                .Where(i => i.EventId == eventId && i.OrganizationId != null)
                .Select(i => i.OrganizationId!.Value)
                .ToListAsync();

            if (organizationIds.Count == 0)
                return false;

            return await _connection.OrganizationMembers
                .AnyAsync(m => m.AccountId == accountId
                    && m.Active
                    && organizationIds.Contains(m.OrganizationId));
        }

        public async Task AssignAsync(Guid eventId, List<Guid> accountIds, List<Guid> organizationIds)
        {
            var organizators = await _connection.Organizators
                .Where(i => i.EventId == eventId).ToListAsync();

            if (accountIds?.Any() ?? false)
            {
                var accountOrganizators = accountIds
                    .Where(i => !organizators.Any(o => o.AccountId == i))
                    .Select(i => new EventOrganizatorDto
                    {
                        AccountId = i,
                        EventId = eventId
                    })
                    .ToList();

                if (accountOrganizators.Any())
                    await _connection.BulkCopyAsync(accountOrganizators);
            }

            if (organizationIds?.Any() ?? false)
            {
                var organizationOrganizators = organizationIds
                    .Where(i => !organizators.Any(o => o.OrganizationId == i))
                    .Select(i => new EventOrganizatorDto
                    {
                        OrganizationId = i,
                        EventId = eventId
                    })
                    .ToList();

                if (organizationOrganizators.Any())
                    await _connection.BulkCopyAsync(organizationOrganizators);
            }
        }
    }
}