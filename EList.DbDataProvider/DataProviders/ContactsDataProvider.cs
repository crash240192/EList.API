using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.ContactData;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class ContactsDataProvider : DataProviderBase, IContactsDataProvider
    {
        public ContactsDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }

        public async Task<Guid> CreateContactTypeAsync(ContactTypeDto item)
        {
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
            throw new NotImplementedException();
        }

        public async Task<ContactTypeDto?> GetContactTypeAsync(Guid id)
        {
            var item = await _connection.ContactTypes.FirstOrDefaultAsync(i => i.Id == id);
            return item;
        }

        public async Task<List<ContactTypeDto>> GetAllContactTypesAsync()
        {
            var items = await _connection.ContactTypes.ToListAsync();
            return items;
        }


        public async Task<Guid> CreateContactAsync(ContactDataDto item)
        {
            var result = (Guid)await _connection.InsertWithIdentityAsync(item);
            return result;
        }

        public async Task<bool> CheckContactIsEmptyAsync(string contactValue, Guid contactType)
        {
            var result = !await _connection.ContactData
                .AnyAsync(i => i.Value == contactValue && i.TypeId == contactType);
            return result;
        }

        public async Task UpdateContactAsync(ContactDataDto item)
        {
            await _connection.ContactData.Where(i => i.Id == item.Id)
                .Set(i => i.Value, item.Value)
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

        public async Task<ContactDataDto?> GetAccountContactAsync(Guid id)
        {
            var result = await _connection.ContactData
                .LoadWith(i => i.ContactType)
                .LoadWith(i => i.AccountRelation)
                .Where(i => i.Id == id)
                .FirstOrDefaultAsync();
            return result;
        }

        public async Task<ContactDataDto?> GetContactAsync(string contactValue)
        {
            var result = await _connection.ContactData
                .LoadWith(i => i.ContactType)
                .LoadWith(i => i.AccountRelation)
                .Where(i => i.Value.ToLower() == contactValue.ToLower())
                .FirstOrDefaultAsync();
            return result;
        }

        public async Task<ContactDataDto?> GetAuthorizationContactAsync(Guid accountId)
        {
            var result = await _connection.ContactData
                .LoadWith(i => i.ContactType)
                .LoadWith(i => i.AccountRelation)
                .Where(i => i.AccountRelation.AccountId == accountId)
                .FirstOrDefaultAsync();
            return result;
        }

        public async Task<List<ContactDataDto>?> GetAccountContactsAsync(Guid accountId)
        {
            var result = await _connection.ContactData
                .LoadWith(i => i.ContactType)
                .LoadWith(i => i.AccountRelation)
                .Where(i => i.AccountRelation.AccountId == accountId)
                .ToListAsync();
            return result;
        }
    }
}
