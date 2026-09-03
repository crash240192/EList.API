using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Security;
using EList.Models.ContactData;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class ContactsDataProvider : DataProviderBase, IContactsDataProvider
    {
        private readonly IFieldEncryptor _fieldEncryptor;

        public ContactsDataProvider(
            IDataConnectionProvider dataConnectionProvider,
            IFieldEncryptor fieldEncryptor) : base(dataConnectionProvider)
        {
            _fieldEncryptor = fieldEncryptor;
        }

        public async Task<Guid> CreateContactTypeAsync(ContactTypeDto item)
        {
            item.Active = true;
            var result = (Guid)await _connection.InsertWithIdentityAsync(item);
            return result;
        }

        public async Task UpdateContactTypeAsync(ContactTypeDto item)
        {
            await _connection.ContactTypes.Where(i => i.Id == item.Id)
                .Set(i => i.Name, item.Name)
                .Set(i => i.LocalizationPath, item.LocalizationPath)
                .Set(i => i.Description, item.Description)
                .Set(i => i.Mask, item.Mask)
                .Set(i => i.AllowNotifications, item.AllowNotifications)
                .UpdateAsync();
        }

        public async Task DeleteContactTypeAsync(Guid id)
        {
            await _connection.ContactTypes.Where(i => i.Id == id)
                .Set(i => i.Active, false)
                .UpdateAsync();
        }

        public async Task<ContactTypeDto?> GetContactTypeAsync(Guid id)
        {
            var item = await _connection.ContactTypes.FirstOrDefaultAsync(i => i.Id == id);
            return item;
        }

        public async Task<List<ContactTypeDto>> GetAllContactTypesAsync()
        {
            var items = await _connection.ContactTypes
                .Where(i => i.Active)
                .ToListAsync();
            return items;
        }


        public async Task<Guid> CreateContactAsync(ContactDataDto item)
        {
            PersonalDataCrypto.EncryptContact(item, _fieldEncryptor);
            var result = (Guid)await _connection.InsertWithIdentityAsync(item);
            return result;
        }

        public async Task<bool> CheckContactIsEmptyAsync(string contactValue, Guid contactType)
        {
            var hash = _fieldEncryptor.BlindIndex(contactValue);
            var existsByHash = !string.IsNullOrEmpty(hash)
                && await _connection.ContactData.AnyAsync(i =>
                    i.TypeId == contactType && i.ValueHash == hash);

            if (existsByHash)
                return false;

            // Legacy plaintext rows (ещё не мигрированы)
            var normalized = _fieldEncryptor.NormalizeContact(contactValue);
            var existsPlain = await _connection.ContactData.AnyAsync(i =>
                i.TypeId == contactType
                && i.ValueHash == null
                && i.Value.ToLower() == normalized);

            return !existsPlain;
        }

        public async Task UpdateContactAsync(ContactDataDto item)
        {
            PersonalDataCrypto.EncryptContact(item, _fieldEncryptor);
            await _connection.ContactData.Where(i => i.Id == item.Id)
                .Set(i => i.Value, item.Value)
                .Set(i => i.ValueHash, item.ValueHash)
                .Set(i => i.TypeId, item.TypeId)
                .Set(i => i.Show, item.Show)
                .UpdateAsync();
        }

        public async Task BindAccountAndContactAsync(Guid accountId, Guid contactId)
        {
            var relation = new ContactAccountRelationDto
            {
                AccountId = accountId,
                ContactId = contactId
            };

            var result = (Guid)await _connection.InsertWithIdentityAsync(relation);
        }

        public async Task BindOrganizationAndContactAsync(Guid organizationId, Guid contactId)
        {
            var relation = new ContactOrganizationRelationDto
            {
                OrganizationId = organizationId,
                ContactId = contactId
            };

            await _connection.InsertWithIdentityAsync(relation);
        }

        public async Task<ContactDataDto?> GetAccountContactAsync(Guid id)
        {
            var result = await _connection.ContactData
                .LoadWith(i => i.ContactType)
                .LoadWith(i => i.AccountRelation)
                .Where(i => i.Id == id)
                .FirstOrDefaultAsync();
            PersonalDataCrypto.DecryptContact(result, _fieldEncryptor);
            return result;
        }

        public async Task<ContactDataDto?> GetOrganizationContactAsync(Guid id)
        {
            var result = await _connection.ContactData
                .LoadWith(i => i.ContactType)
                .LoadWith(i => i.OrganizationRelation)
                .Where(i => i.Id == id && i.OrganizationRelation != null)
                .FirstOrDefaultAsync();
            PersonalDataCrypto.DecryptContact(result, _fieldEncryptor);
            return result;
        }

        public async Task<ContactDataDto?> GetContactAsync(string contactValue)
        {
            var hash = _fieldEncryptor.BlindIndex(contactValue);
            ContactDataDto? result = null;

            if (!string.IsNullOrEmpty(hash))
            {
                result = await _connection.ContactData
                    .LoadWith(i => i.ContactType)
                    .LoadWith(i => i.AccountRelation)
                    .LoadWith(i => i.OrganizationRelation)
                    .Where(i => i.ValueHash == hash)
                    .FirstOrDefaultAsync();
            }

            if (result == null)
            {
                var normalized = _fieldEncryptor.NormalizeContact(contactValue);
                result = await _connection.ContactData
                    .LoadWith(i => i.ContactType)
                    .LoadWith(i => i.AccountRelation)
                    .LoadWith(i => i.OrganizationRelation)
                    .Where(i => i.ValueHash == null && i.Value.ToLower() == normalized)
                    .FirstOrDefaultAsync();
            }

            PersonalDataCrypto.DecryptContact(result, _fieldEncryptor);
            return result;
        }

        public async Task<ContactDataDto?> GetAuthorizationContactAsync(Guid accountId)
        {
            var result = await _connection.ContactData
                .LoadWith(i => i.ContactType)
                .LoadWith(i => i.AccountRelation)
                .Where(i => i.AccountRelation.AccountId == accountId)
                .FirstOrDefaultAsync();
            PersonalDataCrypto.DecryptContact(result, _fieldEncryptor);
            return result;
        }

        public async Task<List<ContactDataDto>?> GetAccountContactsAsync(Guid accountId)
        {
            var result = await _connection.ContactData
                .LoadWith(i => i.ContactType)
                .LoadWith(i => i.AccountRelation)
                .Where(i => i.AccountRelation.AccountId == accountId)
                .ToListAsync();
            result?.ForEach(i => PersonalDataCrypto.DecryptContact(i, _fieldEncryptor));
            return result;
        }

        public async Task<List<ContactDataDto>?> GetOrganizationContactsAsync(Guid organizationId)
        {
            var result = await _connection.ContactData
                .LoadWith(i => i.ContactType)
                .LoadWith(i => i.OrganizationRelation)
                .Where(i => i.OrganizationRelation.OrganizationId == organizationId)
                .ToListAsync();
            result?.ForEach(i => PersonalDataCrypto.DecryptContact(i, _fieldEncryptor));
            return result;
        }
    }
}
