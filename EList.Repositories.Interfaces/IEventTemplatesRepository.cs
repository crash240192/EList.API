using EList.Models.EventTemplates;

namespace EList.Repositories.Interfaces
{
    public interface IEventTemplatesRepository
    {
        Task<Guid> CreateAsync(EventTemplate item);

        Task<EventTemplate?> GetByIdAsync(Guid id);

        Task UpdateAsync(EventTemplate item);

        Task DeleteAsync(Guid id);

        Task<List<EventTemplate>> SearchByAccountIdAsync(Guid accountId, string? name = null);

        Task<List<EventTemplate>> SearchByOrganizationIdAsync(Guid organizationId, string? name = null);
    }
}
