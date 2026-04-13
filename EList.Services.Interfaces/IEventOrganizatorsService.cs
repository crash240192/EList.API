using EList.Common.Models;
using EList.Models.EventOrganizators;

namespace EList.Services.Interfaces
{
    public interface IEventOrganizatorsService
    {
        Task<CommandResult<List<EventOrganizator>>> GetByEventIdAsync(Guid eventId);
    }
}
