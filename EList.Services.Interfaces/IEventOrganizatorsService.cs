using EList.Common.Models;
using EList.Models.EventOrganizators;

namespace EList.Services.Interfaces
{
    public interface IEventOrganizatorsService
    {
        Task<CommandResult<List<EventOrganizator>>> GetByEventIdAsync(Guid eventId);
        Task<CommandResult<EventOrganizator?>> GetByIdAsync(Guid id);
        Task<CommandResult> AssignEventOrganizatorsAsync(Guid eventId, List<Guid> accountIds, List<Guid> organizationIds);
        Task<CommandResult> RemoveOrganizatorAsync(Guid eventId, Guid organizatorId);
        Task<CommandResult<bool>> IsCurrentUserEventOrganizatorAsync(Guid eventId);
    }
}
