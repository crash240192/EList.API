using System.Diagnostics;
using EList.Common.Configuration;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.DbDataProvider.Interfaces;
using EList.Models.Accounts;
using EList.Models.Location;
using EList.Models.Privacy;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using TM.Schedule.API.Attributes;
using ConfigurationManager = EList.Common.Configuration.ConfigurationManager;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/accounts")]
    [LoggerHandlerWebApiFilter]
    public class AccountsController : Controller
    {
        #region logger
        private static readonly NLog.ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Api.Controllers.AccountsController.";
        #endregion

        private readonly IAccountsService _accountsService;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IDataConnectionProvider _connectionProvider;
        private readonly IMediaService _mediaService;
        private readonly IProfilePrivacyService _profilePrivacyService;

        public AccountsController(IAccountsService accountsService,
            ICorrelationIdProvider correlationIdProvider,
            IDataConnectionProvider connectionProvider,
            IMediaService mediaService,
            IProfilePrivacyService profilePrivacyService)
        {
            _accountsService = accountsService;
            _correlationIdProvider = correlationIdProvider;
            _connectionProvider = connectionProvider;
            _mediaService = mediaService;
            _profilePrivacyService = profilePrivacyService;
        }


        /// <summary>
        /// Создание аккаунта
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("create")]
        public async Task<CommandResult> CreateAccountAsync(CreateAccountRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateAccountAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var configurationAllowed = false;
                if (ConfigurationManager.AppSettings.Contains("registrationAllowed"))
                    configurationAllowed = bool.Parse(ConfigurationManager.AppSettings["registrationAllowed"]);

                if (!configurationAllowed)
                    return CommandResult.Fail(ErrorCode.RegistrationForbiden, "Регистрация временно недоступна");

                var result = await _accountsService.CreateAccountAsync(request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                await _connectionProvider.RollbackTransactionAsync();
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Получить информацию о текущем аккаунте
        /// </summary>
        /// <returns></returns>
        [HttpGet("getData")]
        public async Task<CommandResult<Account?>> GetAccountAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAccountAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _accountsService.GetAccountByTokenAsync();

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Обновление координат пользователя
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        [HttpPost("updateLocation")]
        public async Task<CommandResult> UpdateLocationAsync(Location location)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAccountAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                await _connectionProvider.StartNewTransactionAsync();

                var result = await _accountsService.UpdateLocationAsync(location.Latitude, location.Longitude);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                await _connectionProvider.RollbackTransactionAsync();
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Получить информацию об аккаунте
        /// </summary>
        /// <returns></returns>
        [HttpGet("getData/{accountId}")]
        public async Task<CommandResult<Account?>> GetAccountAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAccountAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _accountsService.GetAccountAsync(accountId);

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Удаление (деактивация + анонимизация) текущего аккаунта
        /// </summary>
        [HttpDelete("me")]
        public async Task<CommandResult> DeleteMyAccountAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteMyAccountAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _accountsService.DeleteMyAccountAsync();
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                await _connectionProvider.RollbackTransactionAsync();
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Экспорт персональных данных текущего пользователя (JSON)
        /// </summary>
        [HttpGet("me/export")]
        public async Task<CommandResult<AccountDataExport>> ExportMyDataAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(ExportMyDataAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _accountsService.ExportMyDataAsync();

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Получить настройки приватности текущего аккаунта
        /// </summary>
        [HttpGet("privacy")]
        public async Task<CommandResult<AccountPrivacySettings>> GetPrivacySettingsAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetPrivacySettingsAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, "Method started", null);
                var result = await _profilePrivacyService.GetMySettingsAsync();
                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Обновить настройки приватности текущего аккаунта
        /// </summary>
        [HttpPut("privacy")]
        public async Task<CommandResult<AccountPrivacySettings>> UpdatePrivacySettingsAsync(
            UpdatePrivacySettingsRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdatePrivacySettingsAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, "Method started", null);

                var result = await _profilePrivacyService.UpdateMySettingsAsync(request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                await _connectionProvider.RollbackTransactionAsync();
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Можно ли текущему пользователю приглашать указанный аккаунт (с учётом whoCanInviteMe).
        /// </summary>
        [HttpGet("canInvite/{accountId}")]
        public async Task<CommandResult<CanInviteResult>> CanInviteAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CanInviteAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, "Method started", null);
                var result = await _profilePrivacyService.CanInviteAsync(accountId);
                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Массовая проверка canInvite (для модалки приглашения с события).
        /// </summary>
        [HttpPost("canInvite/batch")]
        public async Task<CommandResult<List<CanInviteResult>>> CanInviteBatchAsync(CanInviteBatchRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CanInviteBatchAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, "Method started", null);
                var result = await _profilePrivacyService.CanInviteBatchAsync(request?.AccountIds);
                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }
    }
}
