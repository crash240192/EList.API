using EList.DbDataProvider.Models;

namespace EList.DbDataProvider.Interfaces
{
    public interface IEventOrganizatorsDataProvider
    {
        Task<Guid> CreateAsync(EventOrganizatorDto request);
        Task UpdateAsync(EventOrganizatorDto request);
        Task<EventOrganizatorDto> GetByIdAsync (Guid id);
        Task<List<EventOrganizatorDto>> GetByEventIdAsync(Guid eventId);
    }
}
