using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Localization;
using EList.Models.ContactData;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using EList.Validators.Interfaces;
using NLog;
using System.Diagnostics;

namespace EList.Services.Impl
{
    public class ContactDataService : IContactsService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.ContactDataService.";
        #endregion

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IContactsRepository _contactDataRepository;
        private readonly IAuthorizationService _authorizationService;
        private readonly IAccountsRepository _accountsRepository;
        private readonly IOrganizationsRepository _organizationsRepository;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly IContactValidator _contactValidator;
        private readonly IContactAccessValidator _contactAccessValidator;

        public ContactDataService(ICorrelationIdProvider correlationIdProvider,
            IContactsRepository contactDataRepository,
            IAuthorizationService authorizationService,
            IAccountsRepository accountsRepository,
            IOrganizationsRepository organizationsRepository,
            IAccountDataHolder accountDataHolder,
            IContactValidator contactValidator,
            IContactAccessValidator contactAccessValidator)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _contactDataRepository = contactDataRepository ?? throw new ArgumentNullException(nameof(contactDataRepository));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
            _organizationsRepository = organizationsRepository ?? throw new ArgumentNullException(nameof(organizationsRepository));
            _accountDataHolder = accountDataHolder;
            _contactValidator = contactValidator ?? throw new ArgumentNullException(nameof(contactValidator));
            _contactAccessValidator = contactAccessValidator ?? throw new ArgumentNullException(nameof(contactAccessValidator));
        }

        #region contact types
        public async Task<CommandResult<Guid?>> CreateContactTypeAsync(ContactTypeRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateContactTypeAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _contactDataRepository.CreateContactTypeAsync(request);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(result);
        }

        public async Task<CommandResult<List<ContactType>>> GetAllContactTypesAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAllContactTypesAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _contactDataRepository.GetAllContactTypesAsync();

            if (result?.Any() ?? false)
                result.ForEach(i => i.Name = Localizator.GetProperty(i.LocalizationPath, i.Name));

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<ContactType>>(result);
        }

        public async Task<CommandResult<ContactType?>> GetContactTypeAsync(Guid id)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAllContactTypesAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _contactDataRepository.GetContactTypeAsync(id);
            result.Name = Localizator.GetProperty(result.LocalizationPath, result.Name);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<ContactType?>(result);
        }

        public async Task<CommandResult> UpdateContactTypeAsync(Guid id, ContactTypeRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateContactTypeAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            await _contactDataRepository.UpdateContactTypeAsync(id, request);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }
        #endregion

        #region contact data
        public async Task<CommandResult<Guid?>> CreateContactAsync(ContactRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateContactAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var validationResult = await _contactValidator.ValidateAsync(
                request,
                ownerAccountId: _accountDataHolder.AccountId);
            if (!validationResult.Success)
                return CommandResult<Guid?>.Fail(validationResult.ErrorCode, validationResult.Message);

            var result = await _contactDataRepository.CreateContactAsync(request);

            await _contactDataRepository.BindAccountAndContactAsync(_accountDataHolder.AccountId.Value, result);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(result);
        }

        public async Task<CommandResult<Guid?>> CreateOrganizationContactAsync(Guid organizationId, ContactRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateOrganizationContactAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<Guid?>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var organization = await _organizationsRepository.GetOrganizationAsync(organizationId);
            if (organization == null)
                return CommandResult<Guid?>.Fail(ErrorCode.OrganizationNotFound, $"Организация с id='{organizationId}' не найдена");

            var isOwnerOrManager = await _organizationsRepository.IsOwnerOrManagerAsync(organizationId, _accountDataHolder.AccountId.Value);
            if (!isOwnerOrManager)
                return CommandResult<Guid?>.Fail(ErrorCode.AccessError, "Добавлять контакты может только владелец или менеджер организации");

            var validationResult = await _contactValidator.ValidateAsync(request);
            if (!validationResult.Success)
                return CommandResult<Guid?>.Fail(validationResult.ErrorCode, validationResult.Message);

            var result = await _contactDataRepository.CreateContactAsync(request);
            await _contactDataRepository.BindOrganizationAndContactAsync(organizationId, result);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(result);
        }

        public async Task<CommandResult> UpdateContactAsync(Guid id, ContactRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateContactAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var contact = await _contactDataRepository.GetAccountContactAsync(id);
            if (contact?.AccountId == null)
                return CommandResult.Fail(ErrorCode.ContactNotFound, "Контакт пользователя не найден");

            var modifyAccess = _contactAccessValidator.CanModifyAccountContact(contact, _accountDataHolder.AccountId!.Value);
            if (!modifyAccess.Success)
                return modifyAccess;

            var validationResult = await _contactValidator.ValidateAsync(
                request,
                existingContact: contact,
                ownerAccountId: contact.AccountId);
            if (!validationResult.Success)
                return validationResult;

            await _contactDataRepository.UpdateContactAsync(id, request);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> UpdateOrganizationContactAsync(Guid id, ContactRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateOrganizationContactAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var contact = await _contactDataRepository.GetOrganizationContactAsync(id);
            if (contact?.OrganizationId == null)
                return CommandResult.Fail(ErrorCode.ContactNotFound, $"Контакт организации с id='{id}' не найден");

            var isOwnerOrManager = await _organizationsRepository.IsOwnerOrManagerAsync(contact.OrganizationId.Value, _accountDataHolder.AccountId.Value);
            if (!isOwnerOrManager)
                return CommandResult.Fail(ErrorCode.AccessError, "Изменять контакты может только владелец или менеджер организации");

            var validationResult = await _contactValidator.ValidateAsync(request, existingContact: contact);
            if (!validationResult.Success)
                return validationResult;

            await _contactDataRepository.UpdateContactAsync(id, request);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<ContactDataItem?>> GetAccountContactAsync(Guid id)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAccountContactAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var contact = await _contactDataRepository.GetAccountContactAsync(id);
            if (contact?.AccountId == null)
                return CommandResult<ContactDataItem?>.Fail(ErrorCode.ContactNotFound, "Контакт пользователя не найден");

            var viewAccess = _contactAccessValidator.CanViewAccountContact(contact, _accountDataHolder.AccountId);
            if (!viewAccess.Success)
                return CommandResult<ContactDataItem?>.Fail(viewAccess.ErrorCode, viewAccess.Message);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<ContactDataItem?>(contact);
        }

        public async Task<CommandResult<List<ContactDataItem>?>> GetAccountContactsAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAccountContactsAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var account = await _accountsRepository.GetAccountAsync(accountId);
            if (account == null)
                return CommandResult<List<ContactDataItem>?>.Fail(ErrorCode.AccountNotFound, $"Аккаунт с id='{accountId}' не найден");

            var contacts = await _contactDataRepository.GetAccountContactsAsync(accountId);
            contacts = _contactAccessValidator.FilterAccountContacts(contacts, accountId, _accountDataHolder.AccountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<ContactDataItem>?>(contacts);
        }

        public async Task<CommandResult<ContactDataItem>> GetAuthorizationContactAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAuthorizationContactAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var account = await _accountsRepository.GetAccountAsync(_accountDataHolder.AccountId.Value);
            if (account == null)
                return CommandResult<ContactDataItem>.Fail(ErrorCode.AccountNotFound, $"Аккаунт с id='{_accountDataHolder.AccountId}' не найден");

            var contact = await _contactDataRepository.GetAuthorizationContactAsync(_accountDataHolder.AccountId.Value);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<ContactDataItem>(contact);
        }

        public async Task<CommandResult<List<ContactDataItem>?>> GetAccountContactsAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAccountContactsAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var contacts = await _contactDataRepository.GetAccountContactsAsync(_accountDataHolder.AccountId!.Value);
            contacts = _contactAccessValidator.FilterAccountContacts(
                contacts,
                _accountDataHolder.AccountId.Value,
                _accountDataHolder.AccountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<ContactDataItem>?>(contacts);
        }

        public async Task<CommandResult<ContactDataItem?>> GetOrganizationContactAsync(Guid id)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetOrganizationContactAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var contact = await _contactDataRepository.GetOrganizationContactAsync(id);
            if (contact?.OrganizationId == null)
                return CommandResult<ContactDataItem?>.Fail(ErrorCode.ContactNotFound, $"Контакт организации с id='{id}' не найден");

            var canManage = _accountDataHolder.AccountId != null
                && await _organizationsRepository.IsOwnerOrManagerAsync(contact.OrganizationId.Value, _accountDataHolder.AccountId.Value);

            if (!canManage && !contact.Show)
                return CommandResult<ContactDataItem?>.Fail(ErrorCode.AccessError, "Контакт недоступен для просмотра");

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<ContactDataItem?>(contact);
        }

        public async Task<CommandResult<List<ContactDataItem>?>> GetOrganizationContactsAsync(Guid organizationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetOrganizationContactsAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var organization = await _organizationsRepository.GetOrganizationAsync(organizationId);
            if (organization == null)
                return CommandResult<List<ContactDataItem>?>.Fail(ErrorCode.OrganizationNotFound, $"Организация с id='{organizationId}' не найдена");

            var contacts = await _contactDataRepository.GetOrganizationContactsAsync(organizationId) ?? new List<ContactDataItem>();

            var canManage = _accountDataHolder.AccountId != null
                && await _organizationsRepository.IsOwnerOrManagerAsync(organizationId, _accountDataHolder.AccountId.Value);

            contacts = _contactAccessValidator.FilterOrganizationContacts(contacts, canManage);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<ContactDataItem>?>(contacts);
        }

        #endregion
    }
}
