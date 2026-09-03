using AutoMapper;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.ContactData;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class ContactsRepository : IContactsRepository
    {
        private readonly IContactsDataProvider _contactDataProvider;
        private readonly IMapper _mapper;

        public ContactsRepository(IContactsDataProvider contactDataProvider, IMapper mapper)
        {
            _contactDataProvider = contactDataProvider;
            _mapper = mapper;
        }

        public async Task<Guid> CreateContactTypeAsync(ContactTypeRequest request)
        {
            var mappedRequest = new ContactTypeDto
            {
                AllowNotifications = request.AllowNotifications,
                Description = request.Description,
                Mask = request.Mask,
                Name = request.Name,
                LocalizationPath = request.LocalizationPath
            };
            var result = await _contactDataProvider.CreateContactTypeAsync(mappedRequest);
            return result;
        }

        public async Task<List<ContactType>> GetAllContactTypesAsync()
        {
            var types = (await _contactDataProvider.GetAllContactTypesAsync())?.ToArray();

            var result = _mapper.Map<ContactTypeDto[], ContactType[]>(types);

            return result.ToList();
        }

        public async Task<ContactType?> GetContactTypeAsync(Guid id)
        {
            var contactType = await _contactDataProvider.GetContactTypeAsync(id);
            var result = _mapper.Map<ContactTypeDto, ContactType>(contactType);
            return result;
        }

        public async Task UpdateContactTypeAsync(Guid id, ContactTypeRequest request)
        {
            var mappedRequest = new ContactTypeDto
            {
                AllowNotifications = request.AllowNotifications,
                Description = request.Description,
                Mask = request.Mask,
                Name = request.Name,
                LocalizationPath = request.LocalizationPath,
                Id = id
            };
            await _contactDataProvider.UpdateContactTypeAsync(mappedRequest);
        }

        public async Task DeleteContactTypeAsync(Guid id)
        {
            await _contactDataProvider.DeleteContactTypeAsync(id);
        }

        public async Task<Guid> CreateContactAsync(ContactRequest request)
        {
            var mappedRequest = new ContactDataDto
            {
                IsAuthorizationContact = request.IsAuthorizationContact,
                Show = request.Show,
                TypeId = request.TypeId,
                Value = request.Value
            };
            var result = await _contactDataProvider.CreateContactAsync(mappedRequest);
            return result;
        }

        public async Task<bool> CheckContactIsEmptyAsync(string contactValue, Guid contactType)
        {
            var result = await _contactDataProvider.CheckContactIsEmptyAsync(contactValue, contactType);
            return result;
        }

        public async Task UpdateContactAsync(Guid id, ContactRequest request)
        {
            var mappedRequest = new ContactDataDto
            {
                IsAuthorizationContact = request.IsAuthorizationContact,
                Show = request.Show,
                TypeId = request.TypeId,
                Value = request.Value,
                Id = id
            };
            await _contactDataProvider.UpdateContactAsync(mappedRequest);
        }

        public async Task BindAccountAndContactAsync(Guid accountId, Guid contactId)
        { 
            await _contactDataProvider.BindAccountAndContactAsync(accountId, contactId);
        }

        public async Task BindOrganizationAndContactAsync(Guid organizationId, Guid contactId)
        {
            await _contactDataProvider.BindOrganizationAndContactAsync(organizationId, contactId);
        }

        public async Task<ContactDataItem?> GetAccountContactAsync(Guid contactId)
        { 
            var contact = await _contactDataProvider.GetAccountContactAsync(contactId);
            var result = _mapper.Map<ContactDataItem>(contact);
            if (result != null)
                result.AccountId = contact?.AccountRelation?.AccountId;
            return result;
        }

        public async Task<ContactDataItem?> GetOrganizationContactAsync(Guid contactId)
        {
            var contact = await _contactDataProvider.GetOrganizationContactAsync(contactId);
            var result = _mapper.Map<ContactDataItem>(contact);
            if (result != null)
                result.OrganizationId = contact?.OrganizationRelation?.OrganizationId;
            return result;
        }

        public async Task<ContactDataItem?> GetContactAsync(string contactValue)
        {
            var contact = await _contactDataProvider.GetContactAsync(contactValue);
            var result = _mapper.Map<ContactDataItem>(contact);
            if (result != null)
            {
                result.AccountId = contact?.AccountRelation?.AccountId;
                result.OrganizationId = contact?.OrganizationRelation?.OrganizationId;
            }
            return result;
        }

        public async Task<ContactDataItem?> GetAuthorizationContactAsync(Guid accountId)
        {
            var contact = await _contactDataProvider.GetAuthorizationContactAsync(accountId);
            var result = _mapper.Map<ContactDataItem>(contact);
            if (result != null)
                result.AccountId = contact?.AccountRelation?.AccountId;
            return result;
        }

        public async Task<List<ContactDataItem>> GetAccountContactsAsync(Guid accountId)
        { 
            var contacts = await _contactDataProvider.GetAccountContactsAsync(accountId);
            var result = contacts?.Select(i =>
            {
                var item = _mapper.Map<ContactDataItem>(i);
                item.AccountId = i.AccountRelation?.AccountId;
                return item;
            }).ToList() ?? new List<ContactDataItem>();
            return result;
        }

        public async Task<List<ContactDataItem>> GetOrganizationContactsAsync(Guid organizationId)
        {
            var contacts = await _contactDataProvider.GetOrganizationContactsAsync(organizationId);
            var result = contacts?.Select(i =>
            {
                var item = _mapper.Map<ContactDataItem>(i);
                item.OrganizationId = i.OrganizationRelation?.OrganizationId;
                return item;
            }).ToList() ?? new List<ContactDataItem>();
            return result;
        }
    }
}
