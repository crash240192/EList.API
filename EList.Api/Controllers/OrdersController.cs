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
    [Authorize]
    [ApiController]
    [Route("/api/orders")]
    [LoggerHandlerWebApiFilter]
    public class OrdersController : ControllerBase
    {
        private readonly IOrdersService _ordersService;
        private readonly IDataConnectionProvider _connectionProvider;

        public OrdersController(
            IOrdersService ordersService, IDataConnectionProvider connectionProvider)
        {
            _ordersService = ordersService;
            _connectionProvider = connectionProvider;
        }

        /// <summary>
        /// Создать заказ на билеты. При Cost=0 оплачивается сразу; иначе возвращается confirmationUrl stub/ЮKassa.
        /// </summary>
        [HttpPost]
        public async Task<CommandResult<CreateOrderResponse>> CreateOrderAsync([FromBody] CreateOrderRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _ordersService.CreateOrderAsync(request);
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
        /// Stub/debug: подтвердить оплату (внутри шлёт synthetic webhook payment.succeeded).
        /// Предпочтительно для отладки также: POST /api/payments/yookassa/stub/simulate-succeeded
        /// или POST /api/payments/yookassa/webhook с телом notification.
        /// </summary>
        [HttpPost("payments/complete")]
        public async Task<CommandResult<OrderResponse>> CompletePaymentAsync([FromBody] CompletePaymentRequest request)
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

        /// <summary>Мои заказы.</summary>
        [HttpGet("my")]
        public async Task<CommandResult<List<OrderResponse>>> GetMyOrdersAsync()
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _ordersService.GetMyOrdersAsync();
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

        /// <summary>Заказ по id.</summary>
        [HttpGet("{orderId}")]
        public async Task<CommandResult<OrderResponse>> GetOrderAsync(Guid orderId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _ordersService.GetOrderAsync(orderId);
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
        /// Покупатель: отменить неоплаченный заказ (Pending/Authorized). Освобождает soft-hold мест.
        /// </summary>
        [HttpPost("{orderId}/cancel")]
        public async Task<CommandResult<OrderResponse>> CancelOrderAsync(Guid orderId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _ordersService.CancelOrderAsync(orderId);
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

        /// <summary>Мои билеты (опционально фильтр по eventId).</summary>
        [HttpGet("tickets/my")]
        public async Task<CommandResult<List<TicketResponse>>> GetMyTicketsAsync([FromQuery] Guid? eventId = null)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _ordersService.GetMyTicketsAsync(eventId);
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

        /// <summary>Билет по коду (владелец или организатор события).</summary>
        [HttpGet("tickets/byCode/{code}")]
        public async Task<CommandResult<TicketResponse>> GetTicketByCodeAsync(string code)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _ordersService.GetTicketByCodeAsync(code);
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
        /// Организатор: проверить билет по коду без изменения статуса.
        /// </summary>
        [HttpPost("tickets/validate")]
        public async Task<CommandResult<TicketResponse>> ValidateTicketAsync([FromBody] TicketCheckInRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _ordersService.ValidateTicketForEventAsync(request);
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
        /// Организатор: check-in — отметить присутствие (issued → used), фиксирует кто и когда.
        /// </summary>
        [HttpPost("tickets/check-in")]
        public async Task<CommandResult<TicketResponse>> CheckInTicketAsync([FromBody] TicketCheckInRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _ordersService.CheckInTicketAsync(request);
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
        /// Owner/Manager: отменить check-in (used → issued). Билетёры — AccessError.
        /// </summary>
        [HttpPost("tickets/undo-check-in")]
        public async Task<CommandResult<TicketResponse>> UndoCheckInTicketAsync([FromBody] TicketCheckInRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _ordersService.UndoCheckInTicketAsync(request);
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
        /// Подарок / передача билета: меняется только holder, buyer заказа прежний.
        /// </summary>
        [HttpPost("tickets/transfer")]
        public async Task<CommandResult<TicketResponse>> TransferTicketAsync([FromBody] TransferTicketRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _ordersService.TransferTicketAsync(request);
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

        /// <summary>Создать возврат по билетам заказа (buyer/holder).</summary>
        [HttpPost("refunds")]
        public async Task<CommandResult<RefundResponse>> CreateRefundAsync([FromBody] CreateRefundRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _ordersService.CreateRefundAsync(request);
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

        /// <summary>Отменить заявку на возврат (пока pending).</summary>
        [HttpPost("refunds/cancel")]
        public async Task<CommandResult<RefundResponse>> CancelRefundAsync([FromBody] CancelRefundRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _ordersService.CancelRefundAsync(request);
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

        /// <summary>Список возвратов по заказу.</summary>
        [HttpGet("{orderId}/refunds")]
        public async Task<CommandResult<List<RefundResponse>>> GetRefundsByOrderAsync(Guid orderId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _ordersService.GetRefundsByOrderAsync(orderId);
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
