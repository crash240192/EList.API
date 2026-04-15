using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Localization;
using EList.Models.ContactData;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
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
        private readonly IAccountDataHolder _accountDataHolder;

        public ContactDataService(ICorrelationIdProvider correlationIdProvider,
            IContactsRepository contactDataRepository,
            IAuthorizationService authorizationService,
            IAccountsRepository accountsRepository,
            IAccountDataHolder accountDataHolder)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _contactDataRepository = contactDataRepository ?? throw new ArgumentNullException(nameof(contactDataRepository));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
            _accountDataHolder = accountDataHolder;
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

            //var validationResult = _contactsValidator.ValidateCreation(request);
            //if (!validationResult.Success)
            //    return CommandResult<Guid>.Fail(validationResult.ErrorCode, validationResult.Message);

            var result = await _contactDataRepository.CreateContactAsync(request);

            await _contactDataRepository.BindAccountAndContactAsync(_accountDataHolder.AccountId, result);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(result);
        }

        public async Task<CommandResult> UpdateContactAsync(Guid id, ContactRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateContactAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            //var validationResult = _contactsValidator.ValidateCreation(request);
            //if (!validationResult.Success)
            //    return CommandResult<Guid>.Fail(validationResult.ErrorCode, validationResult.Message);

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

            //var validationResult = _contactsValidator.ValidateCreation(request);
            //if (!validationResult.Success)
            //    return CommandResult<Guid>.Fail(validationResult.ErrorCode, validationResult.Message);

            var contact = await _contactDataRepository.GetAccountContactAsync(id);

            if (contact.AccountId != _accountDataHolder.AccountId && !contact.Show)
                return CommandResult<ContactDataItem?>.Fail(ErrorCode.AccessError, "Контакт недоступен для просмотра");

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<ContactDataItem?>(contact);
        }

        public async Task<CommandResult<List<ContactDataItem>?>> GetAccountContactsAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAccountContactsAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            //var validationResult = _contactsValidator.ValidateCreation(request);
            //if (!validationResult.Success)
            //    return CommandResult<Guid>.Fail(validationResult.ErrorCode, validationResult.Message);

            var account = await _accountsRepository.GetAccountAsync(accountId);
            if (account == null)
                return CommandResult<List<ContactDataItem>?>.Fail(ErrorCode.AccountNotFound, $"Аккаунт с id='{accountId}' не найден");

            var contacts = await _contactDataRepository.GetAccountContactsAsync(accountId);

            if (accountId != _accountDataHolder.AccountId)
                contacts = contacts.Where(i => i.Show).ToList();

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<ContactDataItem>?>(contacts);
        }

        public async Task<CommandResult<List<ContactDataItem>?>> GetAccountContactsAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAccountContactsAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            //var validationResult = _contactsValidator.ValidateCreation(request);
            //if (!validationResult.Success)
            //    return CommandResult<Guid>.Fail(validationResult.ErrorCode, validationResult.Message);

            var contacts = await _contactDataRepository.GetAccountContactsAsync(_accountDataHolder.AccountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<ContactDataItem>?>(contacts);
        }

        #endregion
    }
}
