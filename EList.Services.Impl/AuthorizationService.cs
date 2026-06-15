using EList.Common.CorrelationId;
using EList.Common.Encryption;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.FilestorageClient;
using EList.Models.Authorization;
using EList.Models.Enums;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using EList.Validators.Interfaces;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json.Linq;
using NLog;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace EList.Services.Impl
{
    public class AuthorizationService : IAuthorizationService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.AuthorizationService.";
        #endregion

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IAuthorizationRepository _authorizationRepository;
        private readonly IAccountsRepository _accountsRepository;
        private readonly IUserDataValidator _userDataValidationService;
        private readonly IContactsRepository _contactsRepository;
        private readonly ISystemNotificationsService _notificationService;
        private readonly IEncryptionTool _encryptionTool;
        private readonly IFilestorageClient _filestorageClient;
        private readonly IAccountDataHolder _accountDataHolder;

        public AuthorizationService(ICorrelationIdProvider correlationIdProvider,
            IAccountsRepository accountsRepository,
            IAuthorizationRepository authorizationRepository,
            IUserDataValidator userDataValidationService,
            IContactsRepository contactsRepository,
            ISystemNotificationsService notificationService,
            IEncryptionTool encryptionTool,
            IFilestorageClient filestorageClient,
            IAccountDataHolder accountDataHolder)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _authorizationRepository = authorizationRepository ?? throw new ArgumentNullException(nameof(authorizationRepository));
            _accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
            _encryptionTool = encryptionTool ?? throw new ArgumentNullException(nameof(encryptionTool));
            _contactsRepository = contactsRepository ?? throw new ArgumentNullException(nameof(contactsRepository));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _filestorageClient = filestorageClient ?? throw new ArgumentNullException();
            _accountDataHolder = accountDataHolder;
        }

        public async Task<CommandResult<AuthorizationResponse>> AuthorizeAsync(string login, string password, string clientHash)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AuthorizeAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var passwordHash = _encryptionTool.CalculateStringHash(password);

            var account = await _accountsRepository.GetAccountAsync(login, passwordHash);

            if (account == null)
            {
                var contactTypes = await _contactsRepository.GetAllContactTypesAsync();
                foreach (var contactType in contactTypes)
                {
                    var regexCheck = Regex.Match(login, contactType.Mask);
                    if (regexCheck.Success)
                    {
                        var loginContact = await _contactsRepository.GetContactAsync(login);
                        if (loginContact != null && loginContact.AccountId != null && loginContact.IsAuthorizationContact)
                        {
                            var accountByContact = await _accountsRepository.GetAccountAsync(loginContact.AccountId.Value);
                            if (accountByContact != null)
                            {
                                account = await _accountsRepository.GetAccountAsync(accountByContact.Login, passwordHash);
                                if (account != null)
                                    break;
                            }
                        }
                    }
                }

                if (account == null)
                    return CommandResult<AuthorizationResponse>.Fail(ErrorCode.AuthenticationError, $"Невероный логин или пароль");
            }
            var tokenSearchResult = await _authorizationRepository.GetAuthorizationDataAsync(account.Id, clientHash);

            _accountDataHolder.Account = account;

            var result = new AuthorizationResponse();
            var commandResult = new CommandResult<AuthorizationResponse>(result);
            var contact = (await _contactsRepository.GetAccountContactsAsync(account.Id))?.FirstOrDefault(i => i.IsAuthorizationContact);

            if (tokenSearchResult == null)
            {
                var tokenId = await _authorizationRepository.CreateTokenAsync(account.Id, clientHash);
                _accountDataHolder.Token = tokenId;
                await _notificationService.NotifyUserByContactAsync(SystemNotificationType.Activation);

                result.Token = tokenId;
                result.ActivationRequired = true;

                commandResult.Message = $"Для активации клиента было выслано уведомление на {contact?.Value}";
                return commandResult;
            }
            else
            {
                _accountDataHolder.Token = tokenSearchResult.Token;
            }

            if (!tokenSearchResult.Active)
            {
                await _notificationService.NotifyUserByContactAsync(SystemNotificationType.Activation);
                result.Token = tokenSearchResult.Token;
                result.ActivationRequired = true;

                commandResult.Message = $"Указанный клиент заблокирован. Для активации клиента было выслано уведомление на {contact?.Value}";
                return commandResult;
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);

            result.Token = tokenSearchResult.Token;
            result.ActivationRequired = false;

            return commandResult;
        }

        public async Task<CommandResult<string>> SendActivationCodeAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SendActivationCodeAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _notificationService.NotifyUserByContactAsync(SystemNotificationType.Activation);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return result;
        }

        public async Task<CommandResult<Authorization?>> GetAuthorizationDataAsync(Guid token)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAuthorizationDataAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _authorizationRepository.GetAuthorizationDataAsync(token);
            if (result == null)
                return CommandResult<Authorization?>.Fail(ErrorCode.AuthorizationDataNotFound, $"Не найден авторизационный токен {_accountDataHolder.Token}");

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Authorization?>(result);
        }

        public async Task<CommandResult<Authorization?>> GetAuthorizationDataAsync(string clientHash)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAuthorizationDataAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _authorizationRepository.GetAuthorizationDataAsync(clientHash);
            if (result == null)
                return CommandResult<Authorization?>.Fail(ErrorCode.AuthorizationDataNotFound, $"Не найден авторизационный токен текущего устройства клиента");

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Authorization?>(result);
        }

        public async Task<CommandResult<Guid>> CreateTokenAsync(string clientHash)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateTokenAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var existingToken = await _authorizationRepository.GetAuthorizationDataAsync(_accountDataHolder.AccountId, clientHash);

            if (existingToken != null)
                return new CommandResult<Guid>(existingToken.Token);

            var result = await _authorizationRepository.CreateTokenAsync(_accountDataHolder.AccountId, clientHash);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid>(result);
        }

        public async Task<CommandResult> ActivateTokenAsync(string activationKey, string clientHash)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(ActivateTokenAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            //var existingToken = await _authorizationRepository.GetAuthorizationDataAsync(clientHash);

            var existingToken = await _authorizationRepository.GetAuthorizationDataAsync(_accountDataHolder.Token);

            if (existingToken == null)
                return CommandResult.Fail(ErrorCode.AuthorizationDataNotFound, $"Не найден авторизационный токен для текущего клиента");

            if (existingToken.ClientHash != clientHash)
                return CommandResult.Fail(ErrorCode.AuthenticationError, "Клиент не подтверждён");

            if (existingToken.ActivationKey != activationKey)
            {
                await _authorizationRepository.DecreaseActivationAttempts(existingToken.Token);
                existingToken.ActivationAttemptsRemaining--;

                if (existingToken.ActivationAttemptsRemaining == 0)
                {
                    await _authorizationRepository.DeactivateTokenAsync(existingToken.Token);

                    await _notificationService.NotifyUserByContactAsync(SystemNotificationType.Activation);

                    var notificationContact = (await _contactsRepository.GetAccountContactsAsync(existingToken.AccountId)).FirstOrDefault(i => i.IsAuthorizationContact);

                    return CommandResult.Fail(ErrorCode.InvalidActivationKey, $"Указан не верный код активации.\r\n Отправлен новый код активации на {notificationContact?.Value}");
                }

                return CommandResult.Fail(ErrorCode.InvalidActivationKey, $"Указан не верный код активации.\r\n Осталось попыток: {existingToken.ActivationAttemptsRemaining}");
            }

            await _authorizationRepository.ActivateTokenAsync(existingToken.Token);

            await _notificationService.NotifyUserByContactAsync(SystemNotificationType.Activation);

            await _filestorageClient.RegisterAuthDataAsync(existingToken.Token, existingToken.AccountId, existingToken.ClientHash);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);

            return CommandResult.OK;
        }


        public async Task<CommandResult> DeactivateTokenAsync(Guid token)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeactivateTokenAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var existiongToken = await _authorizationRepository.GetAuthorizationDataAsync(token);
            if (existiongToken == null)
                return CommandResult.Fail(ErrorCode.AuthorizationDataNotFound, $"Не найден авторизационный токен {token}");

            await _authorizationRepository.DeactivateTokenAsync(token);

            await _filestorageClient.DisableAuthDataAsync(existiongToken.Token, existiongToken.ClientHash);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);

            return CommandResult.OK;
        }
    }
}
