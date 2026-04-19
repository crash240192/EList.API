using EList.DbDataProvider.Models;

namespace EList.DbDataProvider.Interfaces
{
    public interface IContactsDataProvider 
    {
        Task<Guid> CreateContactTypeAsync(ContactTypeDto request);
        Task UpdateContactTypeAsync(ContactTypeDto request);
        Task DeleteContactTypeAsync(Guid id);
        Task<ContactTypeDto?> GetContactTypeAsync(Guid id);
        Task<List<ContactTypeDto>> GetAllContactTypesAsync();

        Task<Guid> CreateContactAsync(ContactDataDto request);
        Task UpdateContactAsync(ContactDataDto request);
        Task BindAccountAndContactAsync(Guid accountId, Guid contactId);
        Task<ContactDataDto?> GetAccountContactAsync(Guid id);
        Task<ContactDataDto?> GetAuthorizationContactAsync(Guid accountId);
        Task<List<ContactDataDto>?> GetAccountContactsAsync(Guid accountId);
    }
}
