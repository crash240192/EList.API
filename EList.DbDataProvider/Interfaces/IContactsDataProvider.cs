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
        Task<bool> CheckContactIsEmptyAsync(string contactValue, Guid contactType);
        Task UpdateContactAsync(ContactDataDto request);
        Task BindAccountAndContactAsync(Guid accountId, Guid contactId);
        Task BindOrganizationAndContactAsync(Guid organizationId, Guid contactId);
        Task<ContactDataDto?> GetAccountContactAsync(Guid id);
        Task<ContactDataDto?> GetOrganizationContactAsync(Guid id);
        Task<ContactDataDto?> GetContactAsync(string contactValue);
        Task<ContactDataDto?> GetAuthorizationContactAsync(Guid accountId);
        Task<List<ContactDataDto>?> GetAccountContactsAsync(Guid accountId);
        Task<List<ContactDataDto>?> GetOrganizationContactsAsync(Guid organizationId);
    }
}
