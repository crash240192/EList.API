using EList.Common.Models;
using EList.Models.Orders;

namespace EList.Services.Interfaces
{
    public interface IOrdersService
    {
        Task<CommandResult<CreateOrderResponse>> CreateOrderAsync(CreateOrderRequest request);

        /// <summary>Подтверждение оплаты (stub / будущий webhook-handler).</summary>
        Task<CommandResult<OrderResponse>> CompletePaymentAsync(CompletePaymentRequest request);

        Task<CommandResult<OrderResponse>> GetOrderAsync(Guid orderId);

        Task<CommandResult<List<OrderResponse>>> GetMyOrdersAsync();

        Task<CommandResult<List<TicketResponse>>> GetMyTicketsAsync(Guid? eventId = null);

        Task<CommandResult<TicketResponse>> GetTicketByCodeAsync(string code);
    }
}
