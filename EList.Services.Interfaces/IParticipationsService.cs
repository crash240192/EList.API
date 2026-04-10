using EList.Common.Models;
using EList.Models.Person;

namespace EList.Services.Interfaces
{
    public interface IParticipationsService
    {
        Task<CommandResult<Guid?>> ParticipateAsync(Guid id);
        Task<CommandResult> LeaveEventAsync(Guid id);
        Task<CommandResult<List<PersonInfo>>> GetEventParticipantsAsync(Guid id);
    }
}
