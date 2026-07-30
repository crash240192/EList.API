using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;

namespace EList.DbDataProvider.Interfaces
{
    public interface IOrdersDataProvider
    {
        #region orders
        Task<Guid> CreateOrderAsync(OrderDto item);
        Task<OrderDto?> GetOrderAsync(Guid id);
        Task<OrderDto?> GetOrderFullAsync(Guid id);
        Task<OrderDto?> GetOrderByIdempotencyKeyAsync(string idempotencyKey);
        Task<OrderDto?> GetOrderByProviderPaymentAsync(PaymentProvider provider, string providerPaymentId);
        Task UpdateOrderStatusAsync(Guid orderId, OrderStatus status);
        Task SetOrderPaidAsync(Guid orderId, DateTimeOffset paidAt);
        Task SetProviderPaymentAsync(Guid orderId, PaymentProvider provider, string providerPaymentId);
        Task<List<OrderDto>> GetOrdersByBuyerAsync(Guid buyerAccountId);
        Task<List<OrderDto>> GetOrdersBySellerOrganizationAsync(Guid organizationId);
        Task<List<OrderDto>> GetOrdersByEventAsync(Guid eventId);
        #endregion

        #region tickets
        Task<Guid> CreateTicketAsync(TicketDto item);
        Task CreateTicketsAsync(List<TicketDto> items);
        Task<TicketDto?> GetTicketAsync(Guid id);
        Task<TicketDto?> GetTicketByCodeAsync(string code);
        Task<List<TicketDto>> GetTicketsByOrderAsync(Guid orderId);
        Task<List<TicketDto>> GetTicketsByHolderAsync(Guid holderAccountId);
        Task<List<TicketDto>> GetTicketsByEventAsync(Guid eventId);
        Task UpdateTicketStatusAsync(Guid ticketId, TicketStatus status);
        Task UpdateTicketsStatusByOrderAsync(Guid orderId, TicketStatus status);
        #endregion

        #region refunds
        Task<Guid> CreateRefundAsync(RefundDto item);
        Task<RefundDto?> GetRefundAsync(Guid id);
        Task<RefundDto?> GetRefundByProviderRefundIdAsync(string providerRefundId);
        Task<List<RefundDto>> GetRefundsByOrderAsync(Guid orderId);
        Task UpdateRefundStatusAsync(Guid refundId, RefundStatus status, string? providerRefundId = null);
        #endregion

        #region webhooks
        Task<Guid> CreateWebhookEventAsync(PaymentWebhookEventDto item);
        Task<PaymentWebhookEventDto?> GetWebhookEventAsync(PaymentProvider provider, string providerEventId);
        Task<bool> ExistsWebhookEventAsync(PaymentProvider provider, string providerEventId);
        Task MarkWebhookProcessedAsync(Guid webhookEventId, Guid? orderId = null);
        Task<List<PaymentWebhookEventDto>> GetUnprocessedWebhookEventsAsync(int limit = 100);
        #endregion
    }
}
