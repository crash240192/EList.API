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

        /// <summary>
        /// Обработка NotificationURL Т-Банка. Идемпотентно по provider_event_id. Ответ контроллера — тело OK.
        /// </summary>
        Task<CommandResult> ProcessTBankWebhookAsync(string rawPayload);

        Task<CommandResult<OrderResponse>> GetOrderAsync(Guid orderId);

        /// <summary>
        /// Покупатель отменяет неоплаченный заказ (Pending/Authorized): Cancel у провайдера + статус Canceled.
        /// </summary>
        Task<CommandResult<OrderResponse>> CancelOrderAsync(Guid orderId);

        /// <summary>
        /// Фоновый TTL: отменить брошенные Pending/Authorized старше указанного возраста.
        /// </summary>
        Task<int> PurgeExpiredPendingOrdersAsync(TimeSpan olderThan, int limit = 50);

        Task<CommandResult<List<OrderResponse>>> GetMyOrdersAsync();

        Task<CommandResult<List<TicketResponse>>> GetMyTicketsAsync(Guid? eventId = null);

        Task<CommandResult<TicketResponse>> GetTicketByCodeAsync(string code);

        /// <summary>Организатор: проверить билет без изменения статуса.</summary>
        Task<CommandResult<TicketResponse>> ValidateTicketForEventAsync(TicketCheckInRequest request);

        /// <summary>Организатор: отметить присутствие (issued → used).</summary>
        Task<CommandResult<TicketResponse>> CheckInTicketAsync(TicketCheckInRequest request);

        /// <summary>Подарок/передача: сменить holder, покупатель заказа не меняется.</summary>
        Task<CommandResult<TicketResponse>> TransferTicketAsync(TransferTicketRequest request);

        /// <summary>Buyer/holder: создать возврат по билетам заказа (issued → refund_pending).</summary>
        Task<CommandResult<RefundResponse>> CreateRefundAsync(CreateRefundRequest request);

        /// <summary>Buyer/holder: отменить заявку на возврат (refund_pending → issued).</summary>
        Task<CommandResult<RefundResponse>> CancelRefundAsync(CancelRefundRequest request);

        /// <summary>Stub/debug: имитация refund.succeeded через webhook-путь.</summary>
        Task<CommandResult<RefundResponse>> CompleteRefundAsync(CompleteRefundRequest request);

        Task<CommandResult<List<RefundResponse>>> GetRefundsByOrderAsync(Guid orderId);
    }
}
