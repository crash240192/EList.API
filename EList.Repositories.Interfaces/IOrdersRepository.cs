using EList.Models.Enums;
using EList.Models.Orders;

namespace EList.Repositories.Interfaces
{
    public interface IOrdersRepository
    {
        #region orders
        Task<Guid> CreateOrderAsync(Order item);
        Task<Order?> GetOrderAsync(Guid id);
        Task<Order?> GetOrderFullAsync(Guid id);
        Task<Order?> GetOrderByIdempotencyKeyAsync(string idempotencyKey);
        Task<Order?> GetOrderByProviderPaymentAsync(PaymentProvider provider, string providerPaymentId);
        Task UpdateOrderStatusAsync(Guid orderId, OrderStatus status);
        Task SetOrderPaidAsync(Guid orderId, DateTimeOffset paidAt);
        Task SetProviderPaymentAsync(Guid orderId, PaymentProvider provider, string providerPaymentId);
        Task<List<Order>> GetOrdersByBuyerAsync(Guid buyerAccountId);
        Task<List<Order>> GetOrdersBySellerOrganizationAsync(Guid organizationId);
        Task<List<Order>> GetOrdersByEventAsync(Guid eventId);
        #endregion

        #region tickets
        Task<Guid> CreateTicketAsync(Ticket item);
        Task CreateTicketsAsync(List<Ticket> items);
        Task<Ticket?> GetTicketAsync(Guid id);
        Task<Ticket?> GetTicketByCodeAsync(string code);
        Task<List<Ticket>> GetTicketsByOrderAsync(Guid orderId);
        Task<List<Ticket>> GetTicketsByHolderAsync(Guid holderAccountId);
        Task<List<Ticket>> GetTicketsByEventAsync(Guid eventId);
        Task UpdateTicketStatusAsync(Guid ticketId, TicketStatus status);
        Task UpdateTicketsStatusByOrderAsync(Guid orderId, TicketStatus status);
        #endregion

        #region refunds
        Task<Guid> CreateRefundAsync(Refund item);
        Task<Refund?> GetRefundAsync(Guid id);
        Task<Refund?> GetRefundByProviderRefundIdAsync(string providerRefundId);
        Task<List<Refund>> GetRefundsByOrderAsync(Guid orderId);
        Task UpdateRefundStatusAsync(Guid refundId, RefundStatus status, string? providerRefundId = null);
        #endregion

        #region webhooks
        Task<Guid> CreateWebhookEventAsync(PaymentWebhookEvent item);
        Task<PaymentWebhookEvent?> GetWebhookEventAsync(PaymentProvider provider, string providerEventId);
        Task<bool> ExistsWebhookEventAsync(PaymentProvider provider, string providerEventId);
        Task MarkWebhookProcessedAsync(Guid webhookEventId, Guid? orderId = null);
        Task<List<PaymentWebhookEvent>> GetUnprocessedWebhookEventsAsync(int limit = 100);
        #endregion
    }
}
