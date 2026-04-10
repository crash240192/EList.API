using EList.Common.Models;
using EList.Models.Person;

namespace EList.Validators.Interfaces
{
    public interface IPersonValidator
    {
        CommandResult ValidateCreation(PersonRequest request);
        Task<CommandResult> ValidateUpdation(Guid accountId, PersonRequest request);
        Task<CommandResult> ValidateAccountExists(Guid accountId);
    }
}