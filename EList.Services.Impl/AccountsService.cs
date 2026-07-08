using System.Diagnostics;
using EList.Common.CorrelationId;
using EList.Common.Encryption;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Accounts;
using EList.Models.ContactData;
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

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IAccountsRepository _accountsRepository;
        private readonly IAuthorizationRepository _authorizationRepository;
        private readonly IContactsRepository _contactsRepository;
        private readonly IUserDataValidator _userDataValidationService;
        private readonly ISystemNotificationsService _notificationsService;
        private readonly IEncryptionTool _encryptionTool;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly IWalletsService _walletsService;
        private readonly IMediaRepository _mediaRepository;

        public AccountsService(ICorrelationIdProvider correlationIdProvider,
            IAccountsRepository accountsRepository,
            IAuthorizationRepository authorizationRepository,
            IContactsRepository contactsRepository,
            IUserDataValidator userDataValidationService,
            ISystemNotificationsService notificationsService,
            IEncryptionTool encryptionTool,
            IWalletsService walletsService,
            IAccountDataHolder accountDataHolder,
            IMediaRepository mediaRepository)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
            _authorizationRepository = authorizationRepository ?? throw new ArgumentNullException(nameof(authorizationRepository));
            _contactsRepository = contactsRepository ?? throw new ArgumentNullException(nameof(contactsRepository));
            _userDataValidationService = userDataValidationService ?? throw new ArgumentNullException(nameof(userDataValidationService));
            _encryptionTool = encryptionTool ?? throw new ArgumentNullException(nameof(encryptionTool));
            _notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
            _walletsService = walletsService ?? throw new ArgumentNullException(nameof(walletsService));
            _mediaRepository = mediaRepository ?? throw new ArgumentNullException(nameof(mediaRepository));
            _accountDataHolder = accountDataHolder;
        }

        public async Task<CommandResult<Guid?>> CreateAccountAsync(CreateAccountRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateAccountAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            //TODO: Валидация контактных данных
            var existingAccount = await _accountsRepository.GetAccountAsync(request.Login);
            if (existingAccount != null)
                return CommandResult<Guid?>.Fail(ErrorCode.DublicateAccount, "Указанный логин уже занят");

            if (request.Password != request.PasswordConfirmation)
                return CommandResult<Guid?>.Fail(ErrorCode.PasswordsDontMatch, "Пароль и подтверждение пароля не совпадают");

            var existingContact = await _contactsRepository.CheckContactIsEmptyAsync(request.AuthorizationContactValue, request.AuthorizationContactType);
            if (!existingContact) 
                return CommandResult<Guid?>.Fail(ErrorCode.AuthorizationContactIsNotEmpty, $"Аккаунт, зарегистрированный на {request.AuthorizationContactValue}, уже существует");

            request.Password = _encryptionTool.CalculateStringHash(request.Password);

            var accountId = await _accountsRepository.CreateAccountAsync(request);
            var account = await _accountsRepository.GetAccountAsync(accountId);
            var tokenId = await _authorizationRepository.CreateTokenAsync(accountId, _accountDataHolder.ClientHash);

            _accountDataHolder.Token = tokenId;
            _accountDataHolder.Account = account;

            var tokenInfo = await _authorizationRepository.GetAuthorizationDataAsync(tokenId);

            var contactId = await _contactsRepository.CreateContactAsync(new ContactRequest
            { 
                 IsAuthorizationContact = true,
                 TypeId = request.AuthorizationContactType,
                 Value = request.AuthorizationContactValue,
                 Show = request.ShowContact
            });

            await _contactsRepository.BindAccountAndContactAsync(accountId, contactId);

            //await _notificationsService.NotifyUserByContactAsync(SystemNotificationType.AccountCreated);

            await _walletsService.CreateAccountWalletAsync(accountId);

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
            //if (result != null)
            //    result.AvatarId = await _mediaRepository.GetLastAccountAvatarAsync(accountId);
            
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
            //if (result != null)
            //    result.AvatarId = await _mediaRepository.GetLastAccountAvatarAsync(authorizationInfo.AccountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Account?>(result);
        }

        public async Task<CommandResult> UpdateLocationAsync(double latitude, double longitude)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateLocationAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var authData = await _authorizationRepository.GetAuthorizationDataAsync(_accountDataHolder.Token.Value);

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
    }
}
