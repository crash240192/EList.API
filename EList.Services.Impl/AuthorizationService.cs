using EList.Common.CorrelationId;
using EList.Common.Encryption;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.FilestorageClient;
using EList.Models.Accounts;
using EList.Models.Authorization;
using EList.Models.Enums;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using EList.Validators.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json.Linq;
using NLog;
using Org.BouncyCastle.Asn1.Ocsp;
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
        private readonly IModerationPenaltiesService _moderationPenaltiesService;

        public AuthorizationService(ICorrelationIdProvider correlationIdProvider,
            IAccountsRepository accountsRepository,
            IAuthorizationRepository authorizationRepository,
            IUserDataValidator userDataValidationService,
            IContactsRepository contactsRepository,
            ISystemNotificationsService notificationService,
            IEncryptionTool encryptionTool,
            IFilestorageClient filestorageClient,
            IAccountDataHolder accountDataHolder,
            IModerationPenaltiesService moderationPenaltiesService)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _authorizationRepository = authorizationRepository ?? throw new ArgumentNullException(nameof(authorizationRepository));
            _accountsRepository = accountsRepository ?? throw new ArgumentNullException(nameof(accountsRepository));
            _encryptionTool = encryptionTool ?? throw new ArgumentNullException(nameof(encryptionTool));
            _contactsRepository = contactsRepository ?? throw new ArgumentNullException(nameof(contactsRepository));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _filestorageClient = filestorageClient ?? throw new ArgumentNullException();
            _accountDataHolder = accountDataHolder;
            _moderationPenaltiesService = moderationPenaltiesService ?? throw new ArgumentNullException(nameof(moderationPenaltiesService));
        }

        public async Task<CommandResult<AuthorizationResponse>> AuthorizeAsync(string login, string password)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AuthorizeAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var passwordHash = _encryptionTool.CalculateStringHash(password);

            var account = await FindAccountByLoginAsync(login, password);

            if (account == null)
                return CommandResult<AuthorizationResponse>.Fail(ErrorCode.AuthenticationError, "Неправильное имя пользователя или пароль");

            if (!account.Active)
            {
                await _moderationPenaltiesService.LiftExpiredForAccountAsync(account.Id);
                account = await _accountsRepository.GetAccountAsync(account.Id);
                if (account == null || !account.Active)
                {
                    var message = await BuildAccountDisabledMessageAsync(account?.Id);
                    return CommandResult<AuthorizationResponse>.Fail(ErrorCode.ModerationPenaltyActive, message);
                }
            }

            var tokenSearchResult = await _authorizationRepository.GetAuthorizationDataAsync(account.Id, _accountDataHolder.ClientHash);

            _accountDataHolder.Account = account;

            var result = new AuthorizationResponse();
            var commandResult = new CommandResult<AuthorizationResponse>(result);
            var contact = (await _contactsRepository.GetAccountContactsAsync(account.Id))?.FirstOrDefault(i => i.IsAuthorizationContact);

            if (tokenSearchResult == null)
            {
                var tokenId = await _authorizationRepository.CreateTokenAsync(account.Id, _accountDataHolder.ClientHash);
                _accountDataHolder.Token = tokenId;
                await _notificationService.NotifyUserByContactAsync(SystemNotificationType.Activation);

                result.Token = tokenId;
                result.ActivationRequired = true;

                commandResult.Message = $"Указанный клиент не активирован. Для активации клиента было выслано уведомление на {contact?.Value}";
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

                commandResult.Message = $"Указанный клиент не активирован. Для активации клиента было выслано уведомление на {contact?.Value}";
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

        public async Task<CommandResult<AuthorizationResponse?>> GetAuthorizationDataAsync(string clientHash)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAuthorizationDataAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _authorizationRepository.GetAuthorizationDataAsync(clientHash);
            if (result == null)
                return CommandResult<AuthorizationResponse?>.Fail(ErrorCode.AuthorizationDataNotFound, $"Не найден авторизационный токен текущего устройства клиента");

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<AuthorizationResponse?>(new AuthorizationResponse
            {
                ActivationRequired = !result.Active,
                Token = result.Token
            });
        }

        [Obsolete]
        public async Task<CommandResult<Guid>> CreateTokenAsync(string clientHash)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateTokenAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var existingToken = await _authorizationRepository.GetAuthorizationDataAsync(_accountDataHolder.AccountId.Value, clientHash);

            if (existingToken != null)
                return new CommandResult<Guid>(existingToken.Token);

            var result = await _authorizationRepository.CreateTokenAsync(_accountDataHolder.AccountId.Value, clientHash);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid>(result);
        }

        public async Task<CommandResult> ActivateTokenAsync(string activationKey)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(ActivateTokenAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            //var existingToken = await _authorizationRepository.GetAuthorizationDataAsync(clientHash);

            var existingToken = await _authorizationRepository.GetAuthorizationDataAsync(_accountDataHolder.Token.Value);

            if (existingToken == null)
                return CommandResult.Fail(ErrorCode.AuthorizationDataNotFound, $"Не найден авторизационный токен для текущего клиента");

            if (existingToken.ClientHash != _accountDataHolder.ClientHash)
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

            var tokenAccount = await _accountsRepository.GetAccountAsync(existingToken.AccountId);
            if (tokenAccount != null && !tokenAccount.Active)
            {
                await _moderationPenaltiesService.LiftExpiredForAccountAsync(tokenAccount.Id);
                tokenAccount = await _accountsRepository.GetAccountAsync(tokenAccount.Id);
                if (tokenAccount == null || !tokenAccount.Active)
                {
                    var message = await BuildAccountDisabledMessageAsync(tokenAccount?.Id);
                    return CommandResult.Fail(ErrorCode.ModerationPenaltyActive, message);
                }
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

        public async Task<CommandResult> ChangePasswordAsync(ChangePasswordRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(ChangePasswordAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var oldPasswordHash = _encryptionTool.CalculateStringHash(request.OldPassword);

            var authData = await _authorizationRepository.GetAuthorizationDataAsync(_accountDataHolder.Token.Value);
            if (authData == null)
                return CommandResult.Fail(ErrorCode.AccountNotFound, "Аккаунт не найден");

            var account = await _accountsRepository.GetAccountAsync(authData.AccountId);

            if (_encryptionTool.CalculateStringHash(request.OldPassword) != account?.PasswordHash)
                return CommandResult.Fail(ErrorCode.PasswordsDontMatch, "Старый пароль указан не верно");

            if (request.NewPassword != request.NewPasswordConfirmation)
                return CommandResult.Fail(ErrorCode.PasswordsDontMatch, "Пароль и подтверждение пароля не совпадают");

            var newPasswordHash = _encryptionTool.CalculateStringHash(request.NewPassword);

            if (newPasswordHash == oldPasswordHash)
                return CommandResult.Fail(ErrorCode.NewAndOldPasswordsMatch, "Новый пароль должен отличаться от старого");

            await _accountsRepository.UpdatePasswordAsync(account.Id, newPasswordHash);

            await _authorizationRepository.DeactivateAccountTokensAsync(account.Id);

            await _authorizationRepository.ActivateTokenAsync(_accountDataHolder.Token.Value);

            await _notificationService.NotifyUserByContactAsync(SystemNotificationType.PasswordHasBeenChanged);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> ForgotPasswordAsync(string login)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(ForgotPasswordAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (string.IsNullOrWhiteSpace(login))
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Логин не указан");

            var account = await FindAccountByLoginAsync(login);

            if (account == null)
                return CommandResult.Fail(ErrorCode.AccountNotFound, "Аккаунт не найден");

            var token = await _authorizationRepository.GetAuthorizationDataAsync(_accountDataHolder.ClientHash);

            if (token == null)
            {
                var newTokenId = await _authorizationRepository.CreateTokenAsync(account.Id, _accountDataHolder.ClientHash);
                _accountDataHolder.Token = newTokenId;
            }
            else
            {
                _accountDataHolder.Token = token.Token;
            }

            await _authorizationRepository.GenerateNewActivationKey(_accountDataHolder.Token.Value);

            await _notificationService.NotifyUserByContactAsync(SystemNotificationType.ResetPasswordRequest, account.Id);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> VerifyResetPasswordAsync(string login, string code)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(VerifyResetPasswordAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (string.IsNullOrWhiteSpace(login))
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Логин не указан");

            if (string.IsNullOrWhiteSpace(code))
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Код авторизации не указан");

            var account = await FindAccountByLoginAsync(login);

            if (account == null)
                return CommandResult.Fail(ErrorCode.AccountNotFound, "Аккаунт не найден");

            var token = await _authorizationRepository.GetAuthorizationDataAsync(_accountDataHolder.ClientHash);

            if (token == null)
                return CommandResult.Fail(ErrorCode.AccountNotFound, "Не найден токен для указанного клиента");
            else
                _accountDataHolder.Token = token.Token;

            if (token.ActivationKey != code)
            {
                await _authorizationRepository.GenerateNewActivationKey(_accountDataHolder.Token.Value);
                await _notificationService.NotifyUserByContactAsync(SystemNotificationType.ResetPasswordRequest, account.Id);
                return CommandResult.Fail(ErrorCode.InvalidActivationKey, "Код сброса пароля указан неправильно. Был отправлен новый код");
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<AuthorizationResponse>> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(VerifyResetPasswordAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (string.IsNullOrWhiteSpace(request?.Login))
                return CommandResult<AuthorizationResponse>.Fail(ErrorCode.IsNullOrEmpty, "Логин не указан");

            if (string.IsNullOrWhiteSpace(request?.Code))
                return CommandResult<AuthorizationResponse>.Fail(ErrorCode.IsNullOrEmpty, "Код авторизации не указан");

            if (string.IsNullOrWhiteSpace(request.NewPassword))
                return CommandResult<AuthorizationResponse>.Fail(ErrorCode.IsNullOrEmpty, "Пароль не должен быть пустым");

            if (string.IsNullOrWhiteSpace(request.NewPasswordConfirmation))
                return CommandResult<AuthorizationResponse>.Fail(ErrorCode.IsNullOrEmpty, "Подтверждение пароля не должно быть пустым");

            if (request.NewPasswordConfirmation != request.NewPassword)
                return CommandResult<AuthorizationResponse>.Fail(ErrorCode.PasswordsDontMatch, "Пароль и подтверждение пароля не совпадают");

            var account = await FindAccountByLoginAsync(request.Login);

            if (account == null)
                return CommandResult<AuthorizationResponse>.Fail(ErrorCode.AccountNotFound, "Аккаунт не найден");

            var token = await _authorizationRepository.GetAuthorizationDataAsync(_accountDataHolder.ClientHash);

            if (token == null)
                return CommandResult<AuthorizationResponse>.Fail(ErrorCode.AccountNotFound, "Не найден токен для указанного клиента");
            else
                _accountDataHolder.Token = token.Token;

            if (token.ActivationKey != request.Code)
            {
                await _authorizationRepository.GenerateNewActivationKey(_accountDataHolder.Token.Value);
                await _notificationService.NotifyUserByContactAsync(SystemNotificationType.ResetPasswordRequest, account.Id);
                return CommandResult<AuthorizationResponse>.Fail(ErrorCode.InvalidActivationKey, "Код сброса пароля указан неправильно. Был отправлен новый код");
            }

            await _authorizationRepository.DeactivateAccountTokensAsync(account.Id);
            await _authorizationRepository.ActivateTokenAsync(token.Token);

            await _filestorageClient.RegisterAuthDataAsync(token.Token, account.Id, _accountDataHolder.ClientHash);

            var newPasswordHash = _encryptionTool.CalculateStringHash(request.NewPassword);
            await _accountsRepository.UpdatePasswordAsync(account.Id, newPasswordHash);

            var result = new AuthorizationResponse
            {
                Token = token.Token,
                ActivationRequired = false
            };

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<AuthorizationResponse>(result);
        }

        /// <summary>
        /// Поиск аккаунта по логину и паролю (в том числе по почте/телефону)
        /// </summary>
        /// <param name="login"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private async Task<Account?> FindAccountByLoginAsync(string login, string password = null)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(FindAccountByLoginAsync)}";

            var account = await _accountsRepository.GetAccountAsync(login);
            var passwordHash = password != null ? _encryptionTool.CalculateStringHash(password) : null;
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
                                if (passwordHash == null)
                                {
                                    account = accountByContact;
                                    break;
                                }
                                else
                                {
                                    account = await _accountsRepository.GetAccountAsync(accountByContact.Login, passwordHash);
                                    if (account != null)
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            if (account == null)
                return null;
            
            if (passwordHash != null)
            {
                if (passwordHash != account.PasswordHash)
                    return null;
            }

            return account;
        }

        private async Task<string> BuildAccountDisabledMessageAsync(Guid? accountId)
        {
            if (accountId == null)
                return "Ваш аккаунт заблокирован.";

            var penalties = await _moderationPenaltiesService.GetActiveForAccountAsync(accountId.Value);
            var suspend = penalties.FirstOrDefault(p =>
                p.PenaltyType == Models.Enums.ModerationPenaltyType.SuspendAccount);

            if (suspend != null)
                return ModerationPenaltiesService.FormatRestrictionMessage(suspend);

            if (penalties.Count > 0)
                return ModerationPenaltiesService.FormatRestrictionMessage(penalties[0]);

            return "Ваш аккаунт заблокирован. Обратитесь в поддержку.";
        }
    }
}
