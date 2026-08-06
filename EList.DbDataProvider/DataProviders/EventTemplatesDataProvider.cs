using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class EventTemplatesDataProvider : DataProviderBase, IEventTemplatesDataProvider
    {
        public EventTemplatesDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }

        public async Task<Guid> CreateAsync(EventTemplateDto item)
        {
            var now = DateTimeOffset.UtcNow;
            item.CreateDate = now;
            item.UpdateDate = now;
            var id = (Guid)await _connection.InsertWithIdentityAsync(item);
            return id;
        }

        public async Task<EventTemplateDto?> GetByIdAsync(Guid id)
        {
            return await _connection.EventTemplates.FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task UpdateAsync(EventTemplateDto item)
        {
            await _connection.EventTemplates.Where(i => i.Id == item.Id)
                .Set(i => i.Name, item.Name)
                .Set(i => i.TemplateBody, item.TemplateBody)
                .Set(i => i.UpdateDate, DateTimeOffset.UtcNow)
                .UpdateAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            await _connection.EventTemplates.Where(i => i.Id == id).DeleteAsync();
        }

        public async Task<List<EventTemplateDto>> SearchByAccountIdAsync(Guid accountId, string? name = null)
        {
            var query = _connection.EventTemplates.Where(i => i.OwnerAccountId == accountId);

            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(i => i.Name.Contains(name));

            return await query.OrderByDescending(i => i.UpdateDate).ToListAsync();
        }

        public async Task<List<EventTemplateDto>> SearchByOrganizationIdAsync(Guid organizationId, string? name = null)
        {
            var query = _connection.EventTemplates.Where(i => i.OwnerOrganizationId == organizationId);

            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(i => i.Name.Contains(name));

            return await query.OrderByDescending(i => i.UpdateDate).ToListAsync();
        }
    }
}
