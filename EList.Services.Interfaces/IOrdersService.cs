using EList.Common.Models;
using EList.Models.Orders;

namespace EList.Services.Interfaces
{
    public interface IOrdersService
    {
        Task<CommandResult<CreateOrderResponse>> CreateOrderAsync(CreateOrderRequest request);

        /// <summary>
        /// Stub/debug: имитация успешной оплаты через тот же путь, что webhook ЮKassa.
        /// </summary>
        Task<CommandResult<OrderResponse>> CompletePaymentAsync(CompletePaymentRequest request);

        /// <summary>
        /// Обработка notification ЮKassa (и stub-симуляции). Идемпотентно по provider_event_id.
        /// </summary>
        Task<CommandResult> ProcessYooKassaWebhookAsync(string rawPayload);

        Task<CommandResult<OrderResponse>> GetOrderAsync(Guid orderId);

        Task<CommandResult<List<OrderResponse>>> GetMyOrdersAsync();

        Task<CommandResult<List<TicketResponse>>> GetMyTicketsAsync(Guid? eventId = null);

        Task<CommandResult<TicketResponse>> GetTicketByCodeAsync(string code);

        /// <summary>Организатор: проверить билет без изменения статуса.</summary>
        Task<CommandResult<TicketResponse>> ValidateTicketForEventAsync(TicketCheckInRequest request);

        /// <summary>Организатор: отметить присутствие (issued → used).</summary>
        Task<CommandResult<TicketResponse>> CheckInTicketAsync(TicketCheckInRequest request);
    }
}
