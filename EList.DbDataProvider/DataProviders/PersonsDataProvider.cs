using EList.DbDataProvider.DataConnections;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class PersonsDataProvider : IPersonsDataProvider
    {
        private readonly IDataConnectionProvider _connectionProvider;
        private ElistDataConnection _connection
        {
            get
            {
                return _connectionProvider.GetConnection();
            }
        }

        public PersonsDataProvider(IDataConnectionProvider dataConnectionProvider)
        {
            _connectionProvider = dataConnectionProvider;
        }

        public async Task<Guid> CreatePersonInfoAsync(PersonInfoDto item)
        {
            var result = (Guid)await _connection.InsertWithIdentityAsync(item);
            return result;
        }

        public async Task<PersonInfoDto?> GetPersonInfoAsync(Guid accountId)
        {
            var result = await _connection.Persons.FirstOrDefaultAsync(i => i.AccountId == accountId);
            return result;
        }

        public async Task UpdatePersonInfoAsync(PersonInfoDto item)
        {
            await _connection.Persons.Where(i => i.AccountId == item.AccountId)
                .Set(i => i.Birthdate, item.Birthdate)
                .Set(i => i.FirstName, item.FirstName)
                .Set(i => i.Gender, item.Gender)
                .Set(i => i.LastName, item.LastName)
                .Set(i => i.Patronymic, item.Patronymic)
                .UpdateAsync();
        }
    }
}
