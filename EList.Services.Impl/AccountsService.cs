using System.Diagnostics;
using EList.Common.CorrelationId;
using EList.Common.Encryption;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Accounts;
using EList.Models.ContactData;
using EList.Models.Enums;
using EList.Models.Events;
using EList.Models.Person;
using EList.Models.Subscriptions;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using EList.Validators.Interfaces;
using NLog;

namespace EList.Services.Impl
{
    public class AccountsService : IAccountsService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.AccountsService.";
        #endregion

        private static readonly DocumentType[] RequiredRegistrationDocuments =
        {
            DocumentType.Policy,
            DocumentType.Consent,
            DocumentType.Agreement
        };

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IAccountsRepository _accountsRepository;
        private readonly IAuthorizationRepository _authorizationRepository;
        private readonly IContactsRepository _contactsRepository;
        private readonly IUserDataValidator _userDataValidationService;
        private readonly IContactValidator _contactValidator;
        private readonly ISystemNotificationsService _notificationsService;
        private readonly IEncryptionTool _encryptionTool;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly IWalletsService _walletsService;
        private readonly IMediaRepository _mediaRepository;
        private readonly IAgreementRepository _agreementRepository;
        private readonly IPersonsRepository _personsRepository;
        private readonly IEventsRepository _eventsRepository;
        private readonly ISubscriptionsRepository _subscriptionsRepository;

        public AccountsService(ICorrelationIdProvider correlationIdProvider,
            IAccountsRepository accountsRepository,
            IAuthorizationRepository authorizationRepository,
            IContactsRepository contactsRepository,
            IUserDataValidator userDataValidationService,
            IContactValidator contactValidator,
            ISystemNotificationsService notificationsService,
            IEncryptionTool encryptionTool,
            IWalletsService walletsService,
            IAccountDataHolder accountDataHolder,
            IMediaRepository mediaRepository,
            IAgreementRepository agreementRepository,
            IPersonsRepository personsRepository,
            IEventsRepository eventsRepository,
            ISubscriptionsRepository subscriptionsRepository)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
            _authorizationRepository = authorizationRepository ?? throw new ArgumentNullException(nameof(authorizationRepository));
            _contactsRepository = contactsRepository ?? throw new ArgumentNullException(nameof(contactsRepository));
            _userDataValidationService = userDataValidationService ?? throw new ArgumentNullException(nameof(userDataValidationService));
            _contactValidator = contactValidator ?? throw new ArgumentNullException(nameof(contactValidator));
            _encryptionTool = encryptionTool ?? throw new ArgumentNullException(nameof(encryptionTool));
            _notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
            _walletsService = walletsService ?? throw new ArgumentNullException(nameof(walletsService));
            _mediaRepository = mediaRepository ?? throw new ArgumentNullException(nameof(mediaRepository));
            _agreementRepository = agreementRepository ?? throw new ArgumentNullException(nameof(agreementRepository));
            _personsRepository = personsRepository ?? throw new ArgumentNullException(nameof(personsRepository));
            _eventsRepository = eventsRepository ?? throw new ArgumentNullException(nameof(eventsRepository));
            _subscriptionsRepository = subscriptionsRepository ?? throw new ArgumentNullException(nameof(subscriptionsRepository));
            _accountDataHolder = accountDataHolder;
        }

        public async Task<CommandResult<Guid?>> CreateAccountAsync(CreateAccountRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateAccountAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (!request.AcceptPolicy || !request.AcceptConsent || !request.AcceptAgreement)
            {
                return CommandResult<Guid?>.Fail(ErrorCode.InvalidValue,
                    "Для регистрации необходимо принять Политику ПДн, Согласие на обработку ПДн и Пользовательское соглашение");
            }

            foreach (var documentType in RequiredRegistrationDocuments)
            {
                var document = await _agreementRepository.GetLatestDocumentAsync(documentType);
                if (document == null)
                {
                    return CommandResult<Guid?>.Fail(ErrorCode.AgreementDocumentNotFound,
                        $"Документ «{documentType}» ещё не загружен администратором. Регистрация временно недоступна.");
                }
            }

            var contactValidation = await _contactValidator.ValidateAsync(
                new ContactRequest
                {
                    TypeId = request.AuthorizationContactType,
                    Value = request.AuthorizationContactValue,
                    IsAuthorizationContact = true,
                    Show = request.ShowContact
                },
                allowAuthorizationContact: true);
            if (!contactValidation.Success)
                return CommandResult<Guid?>.Fail(contactValidation.ErrorCode, contactValidation.Message);

            var existingAccount = await _accountsRepository.GetAccountAsync(request.Login);
            if (existingAccount != null)
                return CommandResult<Guid?>.Fail(ErrorCode.DublicateAccount, "Указанный логин уже занят");

            if (request.Password != request.PasswordConfirmation)
                return CommandResult<Guid?>.Fail(ErrorCode.PasswordsDontMatch, "Пароль и подтверждение пароля не совпадают");

            request.Password = _encryptionTool.CalculateStringHash(request.Password);

            var accountId = await _accountsRepository.CreateAccountAsync(request);
            var account = await _accountsRepository.GetAccountAsync(accountId);
            var tokenId = await _authorizationRepository.CreateTokenAsync(accountId, _accountDataHolder.ClientHash);

            _accountDataHolder.Token = tokenId;
            _accountDataHolder.Account = account;

            var contactId = await _contactsRepository.CreateContactAsync(new ContactRequest
            {
                IsAuthorizationContact = true,
                TypeId = request.AuthorizationContactType,
                Value = request.AuthorizationContactValue,
                Show = request.ShowContact
            });

            await _contactsRepository.BindAccountAndContactAsync(accountId, contactId);

            foreach (var documentType in RequiredRegistrationDocuments)
            {
                var document = await _agreementRepository.GetLatestDocumentAsync(documentType);
                await _agreementRepository.SaveUserAgreementAsync(accountId, document.Id);
            }

            await _walletsService.CreateAccountWalletAsync(accountId);

            var welcomeNotify = await _notificationsService.NotifyUserByContactAsync(
                SystemNotificationType.AccountCreated,
                accountId);
            if (!welcomeNotify.Success)
            {
                logger.Debug(correlationId, null, methodName,
                    $"AccountCreated notify skipped: {welcomeNotify.Message}", null);
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(accountId);
        }

        public async Task<CommandResult<Account?>> GetAccountAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAccountAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _accountsRepository.GetAccountAsync(accountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Account?>(result);
        }

        public async Task<CommandResult<Account?>> GetAccountByTokenAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAccountByTokenAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var authorizationInfo = await _authorizationRepository.GetAuthorizationDataAsync(_accountDataHolder.Token.Value);

            var result = await _accountsRepository.GetAccountAsync(authorizationInfo.AccountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Account?>(result);
        }

        public async Task<CommandResult> UpdateLocationAsync(double latitude, double longitude)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateLocationAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            await _accountsRepository.UpdateLocationAsync(_accountDataHolder.AccountId.Value, latitude, longitude);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> UpdateLoginAsync(string newLogin)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateLoginAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var authData = await _authorizationRepository.GetAuthorizationDataAsync(_accountDataHolder.Token.Value);
            var account = await _accountsRepository.GetAccountAsync(authData.AccountId);

            await _accountsRepository.UpdateLoginAsync(account.Id, newLogin);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> DeleteMyAccountAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteMyAccountAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var accountId = _accountDataHolder.AccountId.Value;

            await _personsRepository.UpdatePersonInfoAsync(accountId, new PersonRequest
            {
                FirstName = "Удалённый",
                LastName = "Пользователь",
                Patronymic = string.Empty,
                Gender = null,
                BirthDate = null
            });

            var contacts = await _contactsRepository.GetAccountContactsAsync(accountId) ?? new List<ContactDataItem>();
            var index = 0;
            foreach (var contact in contacts)
            {
                if (contact.ContactType?.Id == null || contact.ContactType.Id == Guid.Empty)
                    continue;

                await _contactsRepository.UpdateContactAsync(contact.Id, new ContactRequest
                {
                    TypeId = contact.ContactType.Id,
                    Value = $"deleted-{accountId:N}-{index}@invalid.local",
                    Show = false,
                    IsAuthorizationContact = false
                });
                index++;
            }

            await _accountsRepository.UpdateLoginAsync(accountId, $"deleted_{accountId:N}");
            await _accountsRepository.SetAccountActiveAsync(accountId, false);
            await _authorizationRepository.DeactivateAccountTokensAsync(accountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<AccountDataExport>> ExportMyDataAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(ExportMyDataAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<AccountDataExport>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var accountId = _accountDataHolder.AccountId.Value;
            var export = new AccountDataExport
            {
                Account = await _accountsRepository.GetAccountAsync(accountId),
                Person = await _personsRepository.GetPersonInfoAsync(accountId),
                Contacts = await _contactsRepository.GetAccountContactsAsync(accountId) ?? new List<ContactDataItem>()
            };

            var organized = await _eventsRepository.SearchEventsShortAsync(
                new EventsSearchRequest { OrganizatorId = accountId, PageIndex = 0, PageSize = 500 },
                accountId,
                _accountDataHolder.AdultConfirmed);
            export.OrganizedEvents = organized?.Result ?? new List<EventShort>();

            var participating = await _eventsRepository.SearchEventsShortAsync(
                new EventsSearchRequest { ParticipantId = accountId, PageIndex = 0, PageSize = 500 },
                accountId,
                _accountDataHolder.AdultConfirmed);
            export.ParticipatingEvents = participating?.Result ?? new List<EventShort>();

            var subscriptions = await _subscriptionsRepository.GetSubscriptionsAsync(new SubscriptionsSearchRequest
            {
                AccountId = accountId,
                PageIndes = 0,
                PageSize = 500
            });
            export.Subscriptions = subscriptions?.Result ?? new List<Subscription>();

            foreach (var documentType in Enum.GetValues<DocumentType>())
            {
                if (await _agreementRepository.DoesUserAgreedWithLatestDocumentVersion(accountId, documentType))
                    export.AcceptedAgreements.Add(documentType.ToString());
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<AccountDataExport>(export);
        }
    }
}
