using EList.Common.Encryption;
using EList.DbDataProvider.DataConnections;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Security;
using LinqToDB;
using LinqToDB.Async;
using System.Globalization;

namespace EList.DbDataProvider.DataProviders
{
    public class PersonsDataProvider : IPersonsDataProvider
    {
        private readonly IDataConnectionProvider _connectionProvider;
        private readonly IFieldEncryptor _fieldEncryptor;

        private ElistDataConnection _connection => _connectionProvider.GetConnection();

        public PersonsDataProvider(
            IDataConnectionProvider dataConnectionProvider,
            IFieldEncryptor fieldEncryptor)
        {
            _connectionProvider = dataConnectionProvider;
            _fieldEncryptor = fieldEncryptor;
        }

        public async Task<Guid> CreatePersonInfoAsync(PersonInfoDto item)
        {
            // Birthdate may arrive as ISO via repository helper; encrypt all PII fields
            PersonalDataCrypto.EncryptPerson(item, _fieldEncryptor);
            var result = (Guid)await _connection.InsertWithIdentityAsync(item);
            return result;
        }

        public async Task<PersonInfoDto?> GetPersonInfoAsync(Guid accountId)
        {
            var result = await _connection.Persons.FirstOrDefaultAsync(i => i.AccountId == accountId);
            PersonalDataCrypto.DecryptPerson(result, _fieldEncryptor);
            return result;
        }

        public async Task UpdatePersonInfoAsync(PersonInfoDto item)
        {
            PersonalDataCrypto.EncryptPerson(item, _fieldEncryptor);
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
