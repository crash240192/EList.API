using EList.Models.Person;

namespace EList.Repositories.Interfaces
{
    public interface IPersonsRepository
    {
        Task<Guid> CreatePersonInfoAsync(Guid accountId, PersonRequest request);
        Task<PersonInfo?> GetPersonInfoAsync(Guid accountId);
        Task UpdatePersonInfoAsync(Guid accountId, PersonRequest request);
    }
}