using EList.Models.EventOrganizators;

namespace EList.Repositories.Interfaces
{
    public interface IEventOrganizatorsRepository
    {
        Task<Guid> CreateAsync(EventOrganizatorRequest request);
        Task UpdateAsync(Guid id, EventOrganizatorRequest request);
        Task<EventOrganizator?> GetByIdAsync(Guid id);
        Task<List<EventOrganizator>?> GetByEventIdAsync(Guid eventId);
        Task AssignAsync(Guid eventId, List<Guid> accountIds);
    }
}
