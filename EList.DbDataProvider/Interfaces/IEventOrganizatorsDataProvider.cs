using EList.DbDataProvider.Models;

namespace EList.DbDataProvider.Interfaces
{
    public interface IEventOrganizatorsDataProvider
    {
        Task<Guid> CreateAsync(EventOrganizatorDto request);
        Task UpdateAsync(EventOrganizatorDto request);
        Task<EventOrganizatorDto> GetByIdAsync (Guid id);
        Task<List<EventOrganizatorDto>> GetByEventIdAsync(Guid eventId);
        Task<List<Guid>> GetOrganizatorIdsByEventIdAsync(Guid eventId);
        /// <summary>
        /// Прямые организаторы-аккаунты и активные участники организаций-соорганизаторов.
        /// </summary>
        Task<List<Guid>> GetAllOrganizerAccountIdsAsync(Guid eventId);
        Task<bool> IsAccountEventOrganizatorAsync(Guid eventId, Guid accountId);
        Task AssignAsync(Guid eventId, List<Guid> accountIds, List<Guid> organizationIds);
        Task DeleteAsync(Guid id);
    }
}
