using EList.DbDataProvider.Models;

namespace EList.DbDataProvider.Interfaces
{
    public interface IPersonsDataProvider
    {
        Task<Guid> CreatePersonInfoAsync(PersonInfoDto request);
        Task<PersonInfoDto?> GetPersonInfoAsync(Guid accountId);
        Task UpdatePersonInfoAsync(PersonInfoDto request);
    }
}
