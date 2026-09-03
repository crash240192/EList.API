using EList.Common.Models;
using EList.Models.ContactData;

namespace EList.Services.Interfaces
{
    public interface IContactsService
    {
        Task<CommandResult<Guid?>> CreateContactTypeAsync(ContactTypeRequest request);
        Task<CommandResult<ContactType?>> GetContactTypeAsync(Guid id);
        Task<CommandResult> UpdateContactTypeAsync(Guid id, ContactTypeRequest request);
        Task<CommandResult> DeleteContactTypeAsync(Guid id);
        Task<CommandResult<List<ContactType>>> GetAllContactTypesAsync();

        Task<CommandResult<Guid?>> CreateContactAsync(ContactRequest request);
        Task<CommandResult<Guid?>> CreateOrganizationContactAsync(Guid organizationId, ContactRequest request);
        Task<CommandResult> UpdateContactAsync(Guid id, ContactRequest request);
        Task<CommandResult> UpdateOrganizationContactAsync(Guid id, ContactRequest request);
        Task<CommandResult<ContactDataItem?>> GetAccountContactAsync(Guid id);
        Task<CommandResult<ContactDataItem?>> GetOrganizationContactAsync(Guid id);
        Task<CommandResult<List<ContactDataItem>?>> GetAccountContactsAsync(Guid accountId);
        Task<CommandResult<List<ContactDataItem>?>> GetAccountContactsAsync();
        Task<CommandResult<List<ContactDataItem>?>> GetOrganizationContactsAsync(Guid organizationId);
        Task<CommandResult<ContactDataItem>> GetAuthorizationContactAsync();
    }
}
