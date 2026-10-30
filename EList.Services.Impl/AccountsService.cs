using System.Diagnostics;
using EList.Common.CorrelationId;
using EList.Common.Encryption;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.FilestorageClient;
using EList.Models.Accounts;
using EList.Models.ContactData;
using EList.Models.Enums;
using EList.Models.Events;
using EList.Models.Person;
using EList.Models.Privacy;
using EList.Models.Subscriptions;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using EList.Validators.Interfaces;
using NLog;
using FileVisibility = EList.FilestorageClient.FileVisibility;

namespace EList.Services.Impl
{
    public class AccountsService : IAccountsService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.AccountsService.";
        #endregion

        /// <summary>
        /// Документы, которые пользователь обязан принять при регистрации.
        /// Policy — только для ознакомления, в подписание не входит.
        /// </summary>
        private static readonly DocumentType[] RequiredRegistrationDocuments =
        {
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
        private readonly IPersonValidator _personValidator;
        private readonly IEventsRepository _eventsRepository;
        private readonly ISubscriptionsRepository _subscriptionsRepository;
        private readonly IOrganizationSubscriptionsRepository _organizationSubscriptionsRepository;
        private readonly IConversationRepository _conversationRepository;
        private readonly IFilestorageClient _filestorageClient;
        private readonly IProfilePrivacyService _profilePrivacyService;
        private readonly IAccountPrivacyRepository _accountPrivacyRepository;

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
            IPersonValidator personValidator,
            IEventsRepository eventsRepository,
            ISubscriptionsRepository subscriptionsRepository,
            IOrganizationSubscriptionsRepository organizationSubscriptionsRepository,
            IConversationRepository conversationRepository,
            IFilestorageClient filestorageClient,
            IProfilePrivacyService profilePrivacyService,
            IAccountPrivacyRepository accountPrivacyRepository)
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
            _personValidator = personValidator ?? throw new ArgumentNullException(nameof(personValidator));
            _eventsRepository = eventsRepository ?? throw new ArgumentNullException(nameof(eventsRepository));
            _subscriptionsRepository = subscriptionsRepository ?? throw new ArgumentNullException(nameof(subscriptionsRepository));
            _organizationSubscriptionsRepository = organizationSubscriptionsRepository ?? throw new ArgumentNullException(nameof(organizationSubscriptionsRepository));
            _conversationRepository = conversationRepository ?? throw new ArgumentNullException(nameof(conversationRepository));
            _filestorageClient = filestorageClient ?? throw new ArgumentNullException(nameof(filestorageClient));
            _profilePrivacyService = profilePrivacyService ?? throw new ArgumentNullException(nameof(profilePrivacyService));
            _accountPrivacyRepository = accountPrivacyRepository ?? throw new ArgumentNullException(nameof(accountPrivacyRepository));
            _accountDataHolder = accountDataHolder;
        }

        public async Task<CommandResult<Guid?>> CreateAccountAsync(CreateAccountRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateAccountAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (!request.AcceptConsent || !request.AcceptAgreement)
            {
                return CommandResult<Guid?>.Fail(ErrorCode.InvalidValue,
                    "Для регистрации необходимо принять Согласие на обработку ПДн и Пользовательское соглашение");
            }

            var personRequest = new PersonRequest
            {
                FirstName = request.FirstName?.Trim(),
                LastName = request.LastName?.Trim(),
                Patronymic = string.IsNullOrWhiteSpace(request.Patronymic) ? null : request.Patronymic.Trim(),
                Gender = request.Gender,
                BirthDate = request.BirthDate
            };
            var personValidation = _personValidator.ValidateCreation(personRequest);
            if (!personValidation.Success)
                return CommandResult<Guid?>.Fail(personValidation.ErrorCode, personValidation.Message);

            foreach (var documentType in RequiredRegistrationDocuments)
            {
                var document = await _agreementRepository.GetLatestDocumentAsync(documentType);
                if (document == null)
                {
                    return CommandResult<Guid?>.Fail(ErrorCode.AgreementDocumentNotFound,
                        $"Документ «{documentType}» ещё не загружен администратором. Регистрация временно недоступна.");
                }
            }

            if (request.AcceptDistributionConsent)
            {
                var distributionDoc = await _agreementRepository.GetLatestDocumentAsync(DocumentType.DistributionConsent);
                if (distributionDoc == null)
                {
                    return CommandResult<Guid?>.Fail(ErrorCode.AgreementDocumentNotFound,
                        "Документ «DistributionConsent» ещё не загружен администратором. Регистрация временно недоступна.");
                }
            }

            // Без согласия на распространение контакт другим не показываем.
            var showContact = request.AcceptDistributionConsent && request.ShowContact;

            // Один объект: ValidateAsync может нормализовать Value (напр. +7999… → +7 (999) …).
            var authContact = new ContactRequest
            {
                TypeId = request.AuthorizationContactType,
                Value = request.AuthorizationContactValue,
                IsAuthorizationContact = true,
                Show = showContact
            };
            var contactValidation = await _contactValidator.ValidateAsync(
                authContact,
                allowAuthorizationContact: true);
            if (!contactValidation.Success)
                return CommandResult<Guid?>.Fail(contactValidation.ErrorCode, contactValidation.Message);

            request.AuthorizationContactValue = authContact.Value;

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

            var contactId = await _contactsRepository.CreateContactAsync(authContact);

            await _contactsRepository.BindAccountAndContactAsync(accountId, contactId);

            // Профиль (в т.ч. ДР ≥ 14) — в той же TX, чтобы не было аккаунта без возраста.
            await _personsRepository.CreatePersonInfoAsync(accountId, personRequest);

            foreach (var documentType in RequiredRegistrationDocuments)
            {
                var document = await _agreementRepository.GetLatestDocumentAsync(documentType);
                await _agreementRepository.SaveUserAgreementAsync(accountId, document.Id);
            }

            if (request.AcceptDistributionConsent)
            {
                var distributionDoc = await _agreementRepository.GetLatestDocumentAsync(DocumentType.DistributionConsent);
                await _agreementRepository.SaveUserAgreementAsync(accountId, distributionDoc.Id);
            }
            else
            {
                // Без согласия — все аудитории ПДн закрыты (Nobody), в т.ч. фото профиля.
                await _accountPrivacyRepository.UpsertAsync(new AccountPrivacySettings
                {
                    AccountId = accountId,
                    WhoCanInviteMe = PrivacyAudience.Nobody,
                    AgeVisibility = PrivacyAudience.Nobody,
                    GenderVisibility = PrivacyAudience.Nobody,
                    ShowBirthdayToday = false,
                    LocationVisibility = PrivacyAudience.Nobody,
                    ProfilePhotosVisibility = PrivacyAudience.Nobody,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
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

        public async Task<CommandResult> CheckRegistrationAvailabilityAsync(CheckRegistrationAvailabilityRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CheckRegistrationAvailabilityAsync)}";

            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (request == null || string.IsNullOrWhiteSpace(request.Login))
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Введите логин");

            var login = request.Login.Trim();
            if (login.Length < 3)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Логин — не менее 3 символов");

            var existingAccount = await _accountsRepository.GetAccountAsync(login);
            if (existingAccount != null)
                return CommandResult.Fail(ErrorCode.DublicateAccount, "Указанный логин уже занят");

            var authContact = new ContactRequest
            {
                TypeId = request.AuthorizationContactType,
                Value = request.AuthorizationContactValue,
                IsAuthorizationContact = true,
                Show = false
            };
            var contactValidation = await _contactValidator.ValidateAsync(
                authContact,
                allowAuthorizationContact: true);
            if (!contactValidation.Success)
                return CommandResult.Fail(contactValidation.ErrorCode, contactValidation.Message);

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<Account?>> GetAccountAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAccountAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _accountsRepository.GetAccountAsync(accountId);
            if (result != null)
                result = await _profilePrivacyService.ApplyAccountViewPolicyAsync(result, _accountDataHolder.AccountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Account?>(result);
        }

        public async Task<CommandResult<AccountLookupResponse?>> LookupAccountAsync(string loginOrId)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(LookupAccountAsync)}";

            if (_accountDataHolder.AccountId == null)
            {
                return CommandResult<AccountLookupResponse?>.Fail(
                    ErrorCode.UserMustBeAuthorized, "Пользователь не авторизован");
            }

            if (string.IsNullOrWhiteSpace(loginOrId))
            {
                return CommandResult<AccountLookupResponse?>.Fail(
                    ErrorCode.IsNullOrEmpty, "Укажите логин или id аккаунта");
            }

            var query = loginOrId.Trim();
            Account? account = null;
            if (Guid.TryParse(query, out var accountId))
                account = await _accountsRepository.GetAccountAsync(accountId);
            else
                account = await _accountsRepository.GetAccountAsync(query);

            if (account == null || !account.Active)
            {
                return CommandResult<AccountLookupResponse?>.Fail(
                    ErrorCode.AccountNotFound, "Аккаунт не найден");
            }

            if (account.Id == _accountDataHolder.AccountId.Value)
            {
                return CommandResult<AccountLookupResponse?>.Fail(
                    ErrorCode.InvalidValue, "Нельзя выбрать свой аккаунт");
            }

            var person = await _personsRepository.GetPersonInfoAsync(account.Id);
            var viewerId = _accountDataHolder.AccountId;
            var hasDistributionConsent = await _profilePrivacyService.HasPersonalDataDistributionConsentAsync(account.Id);
            var canSeePhotos = await _profilePrivacyService.CanViewerSeeProfilePhotosAsync(account.Id, viewerId);
            var response = new AccountLookupResponse
            {
                Id = account.Id,
                Login = account.Login,
                // Avatar: DistributionConsent + ProfilePhotosVisibility (не только consent).
                AvatarId = canSeePhotos ? account.AvatarId : null,
                // ФИО: без отдельной audience — только DistributionConsent (как PersonAccessValidator).
                FirstName = hasDistributionConsent ? person?.FirstName : null,
                LastName = hasDistributionConsent ? person?.LastName : null
            };

            logger.Debug(correlationId, null, methodName, $"Lookup ok account={account.Id:D}", null);
            return new CommandResult<AccountLookupResponse?>(response);
        }

        public async Task<CommandResult<Account?>> GetAccountByTokenAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAccountByTokenAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var authorizationInfo = await _authorizationRepository.GetAuthorizationDataAsync(_accountDataHolder.Token.Value);
            if (authorizationInfo == null)
                return CommandResult<Account?>.Fail(ErrorCode.AuthorizationDataNotFound, "Не найден авторизационный токен");

            var result = await _accountsRepository.GetAccountAsync(authorizationInfo.AccountId);
            if (result != null)
                result = await _profilePrivacyService.ApplyAccountViewPolicyAsync(result, result.Id);

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

        public async Task<CommandResult<Account?>> SetAccountHeaderAsync(AccountHeaderRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SetAccountHeaderAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<Account?>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            request ??= new AccountHeaderRequest();
            var accountId = _accountDataHolder.AccountId.Value;
            var imageId = request.ImageId == Guid.Empty ? null : request.ImageId;

            double? focusX = null;
            double? focusY = null;
            var blur = false;
            if (imageId != null)
            {
                focusX = ClampFocus(request.FocusX ?? 50);
                focusY = ClampFocus(request.FocusY ?? 50);
                blur = request.Blur;
            }

            await _accountsRepository.SetAccountHeaderAsync(accountId, imageId, focusX, focusY, blur);

            if (imageId != null)
            {
                var settings = await _profilePrivacyService.GetOrDefaultAsync(accountId);
                var hasConsent = await _profilePrivacyService.HasPersonalDataDistributionConsentAsync(accountId);
                var isPrivate = !hasConsent || settings.ProfilePhotosVisibility == PrivacyAudience.Nobody;
                try
                {
                    await _filestorageClient.SetFilesVisibilityAsync(
                        new List<Guid> { imageId.Value },
                        isPrivate ? FileVisibility.Private : FileVisibility.Public);
                }
                catch (Exception ex)
                {
                    logger.Warn(correlationId, null, methodName,
                        $"Не удалось обновить visibility шапки {imageId}: {ex.Message}");
                }
            }

            var account = await _accountsRepository.GetAccountAsync(accountId);
            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Account?>(account);
        }

        public async Task<CommandResult<MyEventPinsResponse>> GetMyEventPinsAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetMyEventPinsAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<MyEventPinsResponse>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var ids = await _accountsRepository.GetEventPinIdsAsync(_accountDataHolder.AccountId.Value);
            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<MyEventPinsResponse>(new MyEventPinsResponse { EventIds = ids });
        }

        public async Task<CommandResult<MyEventPinsResponse>> SetMyEventPinsAsync(MyEventPinsRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SetMyEventPinsAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<MyEventPinsResponse>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var eventIds = request?.EventIds ?? new List<Guid>();
            // Ограничение на разумный размер списка закрепов.
            if (eventIds.Count > 100)
                return CommandResult<MyEventPinsResponse>.Fail(ErrorCode.InvalidValue, "Не более 100 закрепов");

            await _accountsRepository.ReplaceEventPinsAsync(_accountDataHolder.AccountId.Value, eventIds);
            var ids = await _accountsRepository.GetEventPinIdsAsync(_accountDataHolder.AccountId.Value);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<MyEventPinsResponse>(new MyEventPinsResponse { EventIds = ids });
        }

        private static double ClampFocus(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 50;
            if (value < 0)
                return 0;
            if (value > 100)
                return 100;
            return value;
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
                BirthDate = null,
                About = null
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

            // Углублённая очистка: media, сообщения, подписки, geo/password.
            try
            {
                var albums = await _mediaRepository.GetAccountAlbumsAsync(accountId) ?? new List<EList.Models.Media.MediaAlbum>();
                foreach (var album in albums)
                {
                    var files = await _mediaRepository.GetAlbumFilesAsync(album.Id, pageIndex: 0, pageSize: 500);
                    var fileIds = files?.Result?.Select(f => f.Id).ToList() ?? new List<Guid>();
                    if (fileIds.Count > 0
                        && _accountDataHolder.Token != null
                        && !string.IsNullOrEmpty(_accountDataHolder.Jwt))
                    {
                        foreach (var fileId in fileIds)
                        {
                            try
                            {
                                await _filestorageClient.DeleteFileAsync(
                                    fileId, _accountDataHolder.Token.Value, _accountDataHolder.Jwt);
                            }
                            catch (Exception ex)
                            {
                                logger.Warn(correlationId, null, methodName,
                                    $"Не удалось удалить файл {fileId} из filestorage: {ex.Message}");
                            }
                        }
                        await _mediaRepository.DeleteFilesAsync(fileIds);
                    }
                    await _mediaRepository.DeleteAlbumAsync(album.Id);
                }

                var avatarIds = await _mediaRepository.GetAccountAvatarsAsync(accountId) ?? new List<Guid>();
                foreach (var avatarId in avatarIds)
                {
                    if (_accountDataHolder.Token != null && !string.IsNullOrEmpty(_accountDataHolder.Jwt))
                    {
                        try
                        {
                            await _filestorageClient.DeleteFileAsync(
                                avatarId, _accountDataHolder.Token.Value, _accountDataHolder.Jwt);
                        }
                        catch (Exception ex)
                        {
                            logger.Warn(correlationId, null, methodName,
                                $"Не удалось удалить аватар {avatarId} из filestorage: {ex.Message}");
                        }
                    }
                    await _mediaRepository.DeleteAvatarAsync(avatarId);
                }

                var accountBeforeClear = await _accountsRepository.GetAccountAsync(accountId);
                if (accountBeforeClear?.HeaderImageId != null
                    && _accountDataHolder.Token != null
                    && !string.IsNullOrEmpty(_accountDataHolder.Jwt))
                {
                    try
                    {
                        await _filestorageClient.DeleteFileAsync(
                            accountBeforeClear.HeaderImageId.Value,
                            _accountDataHolder.Token.Value,
                            _accountDataHolder.Jwt);
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(correlationId, null, methodName,
                            $"Не удалось удалить шапку профиля из filestorage: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(correlationId, null, methodName,
                    $"Ошибка очистки media при удалении аккаунта: {ex.Message}");
            }

            await _conversationRepository.AnonymizeAccountMessagesAsync(accountId);
            await _subscriptionsRepository.DeleteAllForAccountAsync(accountId);
            await _organizationSubscriptionsRepository.DeleteAllForAccountAsync(accountId);
            await _accountsRepository.ClearSensitiveDataAsync(accountId);

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

            var orgSubscriptions = await _organizationSubscriptionsRepository.GetSubscriptionsAsync(
                new OrganizationSubscriptionsSearchRequest
                {
                    AccountId = accountId,
                    PageIndes = 0,
                    PageSize = 500
                });
            export.OrganizationSubscriptions = orgSubscriptions?.Result ?? new List<OrganizationSubscription>();

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
