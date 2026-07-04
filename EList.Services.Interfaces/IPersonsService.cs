using EList.Common.Models;
using EList.Models.Person;

namespace EList.Services.Interfaces
{
    public interface IPersonsService
    {
        Task<CommandResult<Guid?>> CreatePersonInfoAsync(PersonRequest request);
        Task<CommandResult<PersonInfo?>> GetPersonInfoByAccountIdAsync(Guid accountId);

        Task<CommandResult<PersonInfo?>> GetPersonInfoAsync();
        //Task<CommandResult> UpdatePersonInfoAsync(Guid token, PersonRequest request);
    }
}
