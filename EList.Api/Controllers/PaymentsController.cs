using System.Text;
using EList.Api.Extensions;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Orders;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EList.Api.Controllers
{
    /// <summary>
    /// Webhook ЮKassa и stub-симуляция для отладки до подключения реального кабинета.
    /// </summary>
    [ApiController]
    [LoggerHandlerWebApiFilter]
    public class PaymentsController : ControllerBase
    {
        private readonly IOrdersService _ordersService;
        private readonly IDataConnectionProvider _connectionProvider;

        public PaymentsController(
            IOrdersService ordersService, IDataConnectionProvider connectionProvider)
        {
            _ordersService = ordersService;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Notification endpoint ЮKassa (и stub). Без пользовательской авторизации.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("/api/payments/yookassa/webhook")]
        public async Task<IActionResult> YooKassaWebhookAsync()
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                string rawPayload;
                using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                    rawPayload = await reader.ReadToEndAsync();

                var result = await _ordersService.ProcessYooKassaWebhookAsync(rawPayload);
                if (!result.Success)
                {
                    await _connectionProvider.RollbackTransactionAsync();
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// NotificationURL Т-Банка. Без пользовательской авторизации. Тело ответа — ровно OK.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("/api/payments/tbank/webhook")]
        public async Task<IActionResult> TBankWebhookAsync()
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                string rawPayload;
                using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                    rawPayload = await reader.ReadToEndAsync();

                var result = await _ordersService.ProcessTBankWebhookAsync(rawPayload);
                if (!result.Success)
                {
                    await _connectionProvider.RollbackTransactionAsync();
                    return BadRequest(result);
                }

                return Content("OK", "text/plain", Encoding.UTF8);
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// CloudKassir Receipt notification. HMAC (API Secret). Ответ — {"code":0}.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("/api/payments/cloudkassir/receipt")]
        public async Task<IActionResult> CloudKassirReceiptWebhookAsync()
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                string rawPayload;
                using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
                    rawPayload = await reader.ReadToEndAsync();

                Request.Headers.TryGetValue("Content-HMAC", out var contentHmac);
                Request.Headers.TryGetValue("X-Content-HMAC", out var xContentHmac);

                var result = await _ordersService.ProcessCloudKassirReceiptWebhookAsync(
                    rawPayload,
                    contentHmac.ToString(),
                    xContentHmac.ToString());
                if (!result.Success)
                {
                    await _connectionProvider.RollbackTransactionAsync();
                    return BadRequest(result);
                }

                return Content("{\"code\":0}", "application/json", Encoding.UTF8);
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
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
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _ordersService.CompletePaymentAsync(request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
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
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _ordersService.CompleteRefundAsync(request);
                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
