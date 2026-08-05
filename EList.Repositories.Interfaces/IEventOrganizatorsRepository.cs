using EList.Models.EventOrganizators;

namespace EList.Repositories.Interfaces
{
    public interface IEventOrganizatorsRepository
    {
        Task<Guid> CreateAsync(EventOrganizatorRequest request);
        Task UpdateAsync(Guid id, EventOrganizatorRequest request);
        Task<EventOrganizator?> GetByIdAsync(Guid id);
        Task<List<EventOrganizator>?> GetByEventIdAsync(Guid eventId);
        Task<List<Guid>> GetOrganizatorIdsByEventIdAsync(Guid eventId);
        Task<bool> IsAccountEventOrganizatorAsync(Guid eventId, Guid accountId);
        Task AssignAsync(Guid eventId, List<Guid> accountIds, List<Guid> organizationIds);
    }
}
