using EList.Api.Extensions;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Invitations;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Diagnostics;
using TM.Schedule.API.Attributes;

namespace EList.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("/api/invitations")]
    [LoggerHandlerWebApiFilter]
    public class InvitationsController : Controller
    {
        #region logger
        private static readonly NLog.ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Api.Controllers.InvitationsController.";
        #endregion

        private readonly IInvitationsService _invitationsService;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IDataConnectionProvider _connectionProvider;

        public InvitationsController(IInvitationsService invitationsService,
            ICorrelationIdProvider correlationIdProvider,
            IDataConnectionProvider connectionProvider)
        {
            _invitationsService = invitationsService;
            _correlationIdProvider = correlationIdProvider;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Пригласить пользователей
        /// </summary>
        /// <returns></returns>
        [HttpPost("create")]
        public async Task<CommandResult> CreateInvitationsAsync(CreateInvitationsRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateInvitationsAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);
                var result = await _invitationsService.CreateAsync(request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();
                else
                    await _connectionProvider.CommitTransactionAsync();
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
        /// Список приглашений по токену
        /// </summary>
        /// <returns></returns>
        [HttpGet("userInvitations")]
        public async Task<CommandResult<PagedList<Invitation>>> GetUserInvitationsAsync(int pageIndex = 0, int pageSize = 20)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetUserInvitationsAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _invitationsService.GetUserInvitationsAsync(pageIndex, pageSize);

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
        /// Принять приглашение
        /// </summary>
        /// <returns></returns>
        [HttpGet("accept")]
        public async Task<CommandResult> AcceptAsync(Guid invitationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AcceptAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _invitationsService.AcceptAsync(invitationId);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();
                else
                    await _connectionProvider.CommitTransactionAsync();
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
        /// Отклонить приглашение
        /// </summary>
        /// <returns></returns>
        [HttpGet("decline")]
        public async Task<CommandResult> DeclineAsync(Guid invitationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeclineAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _invitationsService.DeclineAsync(invitationId);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();
                else
                    await _connectionProvider.CommitTransactionAsync();
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
        /// Отменить высланное приглашение
        /// </summary>
        /// <returns></returns>
        [HttpGet("cancel")]
        public async Task<CommandResult> CancelAsync(Guid invitationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CancelAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _invitationsService.CancelInvitationAsync(invitationId);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();
                else
                    await _connectionProvider.CommitTransactionAsync();
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
        /// Поиск приглашений
        /// </summary>
        /// <returns></returns>
        [HttpPost("search")]
        public async Task<CommandResult> SearchAsync(InvitationsSearchRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SearchAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _invitationsService.SearchAsync(request);

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
        /// Получить количество непросмотренных приглашений
        /// </summary>
        /// <returns></returns>
        [HttpGet("notViewedCount")]
        public async Task<CommandResult<int>> GetNotViewedInvitationsCountAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetNotViewedInvitationsCountAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _invitationsService.GetNotViewedInvitationsCountAsync();

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
        /// Пометить как просмотренное
        /// </summary>
        /// <param name="invitationId"></param>
        /// <returns></returns>
        [HttpGet("markViewed/{invitationId}")]
        public async Task<CommandResult> ViewInvitationAsync(Guid invitationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(ViewInvitationAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _invitationsService.ViewInvitationAsync(invitationId);

                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
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
