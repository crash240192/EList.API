using EList.Models.ContactData;

namespace EList.Repositories.Interfaces
{
    public interface IContactsRepository
    {
        Task<Guid> CreateContactTypeAsync(ContactTypeRequest request);
        Task<List<ContactType>> GetAllContactTypesAsync();
        Task<ContactType?> GetContactTypeAsync(Guid id);
        Task UpdateContactTypeAsync(Guid id, ContactTypeRequest request);

        Task<Guid> CreateContactAsync(ContactRequest request);
        Task UpdateContactAsync(Guid id, ContactRequest request);
        Task BindAccountAndContactAsync(Guid accountId, Guid contactId);
        Task<ContactDataItem?> GetAccountContactAsync(Guid id);
        Task<List<ContactDataItem>> GetAccountContactsAsync(Guid accountId);
    }
}
