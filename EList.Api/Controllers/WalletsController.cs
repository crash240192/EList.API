using EList.Api.Extensions;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Accounts;
using EList.Models.Wallets;
using EList.Services.Impl;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System.Diagnostics;
using TM.Schedule.API.Attributes;

namespace EList.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    [LoggerHandlerWebApiFilter]
    public class WalletsController : ControllerBase
    {
        #region logger
        private static readonly NLog.ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Api.Controllers.WalletsController.";
        #endregion

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IDataConnectionProvider _connectionProvider;
        private readonly IWalletsService _walletsService;

        public WalletsController(ICorrelationIdProvider correlationIdProvider,
            IWalletsService walletsService,
            IDataConnectionProvider connectionProvider)
        {
            _walletsService = walletsService;
            _correlationIdProvider = correlationIdProvider;
            _connectionProvider = connectionProvider;
        }

        #region tariff
        /// <summary>
        /// Создание тарифа
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("tariff/create")]
        public async Task<CommandResult<Guid?>> CreateTariffAsync(Tariff request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateTariffAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _walletsService.CreateTariffAsync(request);
                if (!result.Success)
                {
                    await _connectionProvider.RollbackTransactionAsync();
                    return CommandResult<Guid?>.Fail(result.ErrorCode, result.Message);
                }

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
        /// Обновление тарифа
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("tariff/update")]
        public async Task<CommandResult> UpdateTariffAsync(Tariff request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateTariffAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _walletsService.UpdateTariffAsync(request);
                if (!result.Success)
                {
                    await _connectionProvider.RollbackTransactionAsync();
                    return CommandResult.Fail(result.ErrorCode, result.Message);
                }

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return CommandResult.OK;

            }
            catch (Exception ex)
            {
                await _connectionProvider.RollbackTransactionAsync();
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Получение тарифа
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("tariff/{tariffId}")]
        public async Task<CommandResult<Tariff?>> GetTariffAsync(Guid tariffId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetTariffAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _walletsService.GetTariffAsync(tariffId);
             
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
        /// Получение тарифа кошелька
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("tariff/byWalletId/{walletId}")]
        public async Task<CommandResult<Tariff?>> GetWalletTariffAsync(Guid walletId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetWalletTariffAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _walletsService.GetWalletTariffAsync(walletId);

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;

            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }
        #endregion


        #region tariff validator
        /// <summary>
        /// Создание валидатора тарифа
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("tariffValidator/create")]
        public async Task<CommandResult<Guid?>> CreateTariffValidatorAsync(TariffValidator request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateTariffValidatorAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _walletsService.CreateTariffValidatorAsync(request);
                if (!result.Success)
                {
                    await _connectionProvider.RollbackTransactionAsync();
                    return CommandResult<Guid?>.Fail(result.ErrorCode, result.Message);
                }

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
        /// Обновление валидатора тарифа
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("tariffValidator/update")]
        public async Task<CommandResult> UpdateTariffValidatorAsync(TariffValidator request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateTariffValidatorAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _walletsService.UpdateTariffValidatorAsync(request);
                if (!result.Success)
                {
                    await _connectionProvider.RollbackTransactionAsync();
                    return CommandResult.Fail(result.ErrorCode, result.Message);
                }

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return CommandResult.OK;

            }
            catch (Exception ex)
            {
                await _connectionProvider.RollbackTransactionAsync();
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }


        /// <summary>
        /// Получение валидатора тарифа
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("tariffValidator/{tariffValidatorId}")]
        public async Task<CommandResult<TariffValidator?>> GetTariffValidatorAsync(Guid tariffValidatorId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetTariffValidatorAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _walletsService.GetTariffValidatorAsync(tariffValidatorId);

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
        /// Получение валидатора тарифа по tariffId
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("tariffValidator/byTariffId/{tariffId}")]
        public async Task<CommandResult<TariffValidator?>> GetTariffValidatorByTariffIdAsync(Guid tariffId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetTariffValidatorByTariffIdAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _walletsService.GetTariffValidatorByTariffIdAsync(tariffId);

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;

            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }
        #endregion

        #region wallets
        /// <summary>
        /// Создание кошелька аккаунта
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("create")]
        public async Task<CommandResult<Guid?>> CreateAccountWalletAsync(Wallet request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateAccountWalletAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _walletsService.CreateAccountWalletAsync(request);
                if (!result.Success)
                {
                    await _connectionProvider.RollbackTransactionAsync();
                    return CommandResult<Guid?>.Fail(result.ErrorCode, result.Message);
                }

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
        /// Создание кошелька организации
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("byOrganization/create")]
        public async Task<CommandResult<Guid?>> CreateOrganizationWalletAsync(Wallet request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateOrganizationWalletAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _walletsService.CreateOrganizationWalletAsync(request);
                if (!result.Success)
                {
                    await _connectionProvider.RollbackTransactionAsync();
                    return CommandResult<Guid?>.Fail(result.ErrorCode, result.Message);
                }

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
        /// Изменение тарифа кошелька
        /// </summary>
        /// <returns></returns>
        [HttpPut("setTariff")]
        public async Task<CommandResult> SetWalletTariffAsync(Guid walletId, Guid tariffId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SetWalletTariffAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _walletsService.SetWalletTariffAsync(walletId, tariffId);
                if (!result.Success)
                {
                    await _connectionProvider.RollbackTransactionAsync();
                    return CommandResult<Guid?>.Fail(result.ErrorCode, result.Message);
                }

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
        /// Получение данных кошелька
        /// </summary>
        /// <returns></returns>
        [HttpGet("/{walletId}")]
        public async Task<CommandResult<Wallet?>> GetWalletAsync(Guid walletId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetWalletAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _walletsService.GetWalletAsync(walletId);
                
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
        /// Получение данных кошелька
        /// </summary>
        /// <returns></returns>
        [HttpGet("byAccount/{accountId}")]
        public async Task<CommandResult<Wallet?>> GetAccountWalletAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAccountWalletAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _walletsService.GetAccountWalletAsync(accountId);

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
        /// Получение данных кошелька
        /// </summary>
        /// <returns></returns>
        [HttpGet("byOrganization/{organizationId}")]
        public async Task<CommandResult<Wallet?>> GetOrganizationWalletAsync(Guid organizationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetOrganizationWalletAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _walletsService.GetOrganizationWalletAsync(organizationId);

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;

            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }
        #endregion
    }
}
