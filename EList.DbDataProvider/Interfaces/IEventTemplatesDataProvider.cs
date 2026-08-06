using EList.DbDataProvider.Models;

namespace EList.DbDataProvider.Interfaces
{
    public interface IEventTemplatesDataProvider
    {
        Task<Guid> CreateAsync(EventTemplateDto item);

        Task<EventTemplateDto?> GetByIdAsync(Guid id);

        Task UpdateAsync(EventTemplateDto item);

        Task DeleteAsync(Guid id);

        Task<List<EventTemplateDto>> SearchByAccountIdAsync(Guid accountId, string? name = null);

        Task<List<EventTemplateDto>> SearchByOrganizationIdAsync(Guid organizationId, string? name = null);
    }
}
