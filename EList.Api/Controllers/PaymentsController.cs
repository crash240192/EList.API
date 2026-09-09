using System.Diagnostics;
using System.Text;
using EList.Api.Extensions;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Orders;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using TM.Schedule.API.Attributes;

namespace EList.Api.Controllers
{
    /// <summary>
    /// Webhook ЮKassa и stub-симуляция для отладки до подключения реального кабинета.
    /// </summary>
    [ApiController]
    [LoggerHandlerWebApiFilter]
    public class PaymentsController : ControllerBase
    {
        #region logger
        private static readonly NLog.ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Api.Controllers.PaymentsController.";
        #endregion

        private readonly IOrdersService _ordersService;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IDataConnectionProvider _connectionProvider;

        public PaymentsController(
            IOrdersService ordersService,
            ICorrelationIdProvider correlationIdProvider,
            IDataConnectionProvider connectionProvider)
        {
            _ordersService = ordersService;
            _correlationIdProvider = correlationIdProvider;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Notification endpoint ЮKassa (и stub). Без пользовательской авторизации.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("/api/payments/yookassa/webhook")]
        public async Task<IActionResult> YooKassaWebhookAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(YooKassaWebhookAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, "Method started", null);

                string rawPayload;
                using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                    rawPayload = await reader.ReadToEndAsync();

                var result = await _ordersService.ProcessYooKassaWebhookAsync(rawPayload);
                if (!result.Success)
                {
                    await _connectionProvider.RollbackTransactionAsync();
                    return BadRequest(result);
                }

                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return Ok(result);
            }
            catch (Exception ex)
            {
                await _connectionProvider.RollbackTransactionAsync();
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Отладка stub: имитировать payment.succeeded webhook (тот же путь, что у реальной ЮKassa).
        /// </summary>
        [Authorize]
        [HttpPost("/api/payments/yookassa/stub/simulate-succeeded")]
        public async Task<CommandResult<OrderResponse>> SimulateSucceededAsync([FromBody] CompletePaymentRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SimulateSucceededAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, "Method started", null);

                var result = await _ordersService.CompletePaymentAsync(request);
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
        /// Отладка stub: имитировать refund.succeeded webhook.
        /// </summary>
        [Authorize]
        [HttpPost("/api/payments/yookassa/stub/simulate-refund-succeeded")]
        public async Task<CommandResult<RefundResponse>> SimulateRefundSucceededAsync(
            [FromBody] CompleteRefundRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SimulateRefundSucceededAsync)}";

            try
            {
                await _connectionProvider.StartNewTransactionAsync();
                logger.Debug(correlationId, null, methodName, "Method started", null);

                var result = await _ordersService.CompleteRefundAsync(request);
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
    }
}
