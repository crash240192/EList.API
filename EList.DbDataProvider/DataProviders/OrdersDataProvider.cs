using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;

namespace EList.DbDataProvider.DataProviders
{
    public class OrdersDataProvider : DataProviderBase, IOrdersDataProvider
    {
        public OrdersDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }

        #region orders
        public async Task<Guid> CreateOrderAsync(OrderDto item)
        {
            item.CreateDate = DateTimeOffset.Now.ToUniversalTime();
            if (string.IsNullOrWhiteSpace(item.Currency))
                item.Currency = "RUB";
            var id = (Guid)await _connection.InsertWithIdentityAsync(item);
            return id;
        }

        public async Task<OrderDto?> GetOrderAsync(Guid id)
        {
            var result = await _connection.Orders.FirstOrDefaultAsync(i => i.Id == id);
            return result;
        }

        public async Task<OrderDto?> GetOrderFullAsync(Guid id)
        {
            var result = await _connection.Orders
                .LoadWith(i => i.Event)
                .LoadWith(i => i.BuyerAccount)
                .ThenLoad(i => i.PersonInfo)
                .LoadWith(i => i.SellerOrganization)
                .LoadWith(i => i.Tickets)
                .LoadWith(i => i.Refunds)
                .FirstOrDefaultAsync(i => i.Id == id);
            return result;
        }

        public async Task<OrderDto?> GetOrderByIdempotencyKeyAsync(string idempotencyKey)
        {
            var result = await _connection.Orders.FirstOrDefaultAsync(i => i.IdempotencyKey == idempotencyKey);
            return result;
        }

        public async Task<OrderDto?> GetOrderByProviderPaymentAsync(PaymentProvider provider, string providerPaymentId)
        {
            var result = await _connection.Orders
                .FirstOrDefaultAsync(i => i.Provider == provider && i.ProviderPaymentId == providerPaymentId);
            return result;
        }

        public async Task UpdateOrderStatusAsync(Guid orderId, OrderStatus status)
        {
            await _connection.Orders.Where(i => i.Id == orderId)
                .Set(i => i.Status, status)
                .UpdateAsync();
        }

        public async Task SetOrderPaidAsync(Guid orderId, DateTimeOffset paidAt)
        {
            await _connection.Orders.Where(i => i.Id == orderId)
                .Set(i => i.Status, OrderStatus.Paid)
                .Set(i => i.PaidAt, paidAt)
                .UpdateAsync();
        }

        public async Task SetProviderPaymentAsync(Guid orderId, PaymentProvider provider, string providerPaymentId)
        {
            await _connection.Orders.Where(i => i.Id == orderId)
                .Set(i => i.Provider, provider)
                .Set(i => i.ProviderPaymentId, providerPaymentId)
                .UpdateAsync();
        }

        public async Task<List<OrderDto>> GetOrdersByBuyerAsync(Guid buyerAccountId)
        {
            var result = await _connection.Orders
                .Where(i => i.BuyerAccountId == buyerAccountId)
                .OrderByDescending(i => i.CreateDate)
                .ToListAsync();
            return result;
        }

        public async Task<List<OrderDto>> GetOrdersBySellerOrganizationAsync(Guid organizationId)
        {
            var result = await _connection.Orders
                .Where(i => i.SellerOrganizationId == organizationId)
                .OrderByDescending(i => i.CreateDate)
                .ToListAsync();
            return result;
        }

        public async Task<List<OrderDto>> GetOrdersByEventAsync(Guid eventId)
        {
            var result = await _connection.Orders
                .Where(i => i.EventId == eventId)
                .OrderByDescending(i => i.CreateDate)
                .ToListAsync();
            return result;
        }
        #endregion

        #region tickets
        public async Task<Guid> CreateTicketAsync(TicketDto item)
        {
            item.IssuedAt = DateTimeOffset.Now.ToUniversalTime();
            var id = (Guid)await _connection.InsertWithIdentityAsync(item);
            return id;
        }

        public async Task CreateTicketsAsync(List<TicketDto> items)
        {
            if (!(items?.Any() ?? false))
                return;

            var now = DateTimeOffset.Now.ToUniversalTime();
            items.ForEach(i => i.IssuedAt = now);
            await _connection.BulkCopyAsync(items);
        }

        public async Task<TicketDto?> GetTicketAsync(Guid id)
        {
            var result = await _connection.Tickets.FirstOrDefaultAsync(i => i.Id == id);
            return result;
        }

        public async Task<TicketDto?> GetTicketByCodeAsync(string code)
        {
            var result = await _connection.Tickets
                .LoadWith(i => i.Order)
                .LoadWith(i => i.Event)
                .LoadWith(i => i.HolderAccount)
                .FirstOrDefaultAsync(i => i.Code == code);
            return result;
        }

        public async Task<List<TicketDto>> GetTicketsByOrderAsync(Guid orderId)
        {
            var result = await _connection.Tickets
                .Where(i => i.OrderId == orderId)
                .OrderBy(i => i.IssuedAt)
                .ToListAsync();
            return result;
        }

        public async Task<List<TicketDto>> GetTicketsByHolderAsync(Guid holderAccountId)
        {
            var result = await _connection.Tickets
                .LoadWith(i => i.Event)
                .Where(i => i.HolderAccountId == holderAccountId)
                .OrderByDescending(i => i.IssuedAt)
                .ToListAsync();
            return result;
        }

        public async Task<List<TicketDto>> GetTicketsByEventAsync(Guid eventId)
        {
            var result = await _connection.Tickets
                .Where(i => i.EventId == eventId)
                .OrderByDescending(i => i.IssuedAt)
                .ToListAsync();
            return result;
        }

        public async Task UpdateTicketStatusAsync(Guid ticketId, TicketStatus status)
        {
            await _connection.Tickets.Where(i => i.Id == ticketId)
                .Set(i => i.Status, status)
                .UpdateAsync();
        }

        public async Task UpdateTicketsStatusByOrderAsync(Guid orderId, TicketStatus status)
        {
            await _connection.Tickets.Where(i => i.OrderId == orderId)
                .Set(i => i.Status, status)
                .UpdateAsync();
        }
        #endregion

        #region refunds
        public async Task<Guid> CreateRefundAsync(RefundDto item)
        {
            item.CreateDate = DateTimeOffset.Now.ToUniversalTime();
            var id = (Guid)await _connection.InsertWithIdentityAsync(item);
            return id;
        }

        public async Task<RefundDto?> GetRefundAsync(Guid id)
        {
            var result = await _connection.Refunds.FirstOrDefaultAsync(i => i.Id == id);
            return result;
        }

        public async Task<RefundDto?> GetRefundByProviderRefundIdAsync(string providerRefundId)
        {
            var result = await _connection.Refunds.FirstOrDefaultAsync(i => i.ProviderRefundId == providerRefundId);
            return result;
        }

        public async Task<List<RefundDto>> GetRefundsByOrderAsync(Guid orderId)
        {
            var result = await _connection.Refunds
                .Where(i => i.OrderId == orderId)
                .OrderByDescending(i => i.CreateDate)
                .ToListAsync();
            return result;
        }

        public async Task UpdateRefundStatusAsync(Guid refundId, RefundStatus status, string? providerRefundId = null)
        {
            var query = _connection.Refunds.Where(i => i.Id == refundId)
                .Set(i => i.Status, status);

            if (providerRefundId != null)
                query = query.Set(i => i.ProviderRefundId, providerRefundId);

            await query.UpdateAsync();
        }
        #endregion

        #region webhooks
        public async Task<Guid> CreateWebhookEventAsync(PaymentWebhookEventDto item)
        {
            item.ReceivedAt = DateTimeOffset.Now.ToUniversalTime();
            var id = (Guid)await _connection.InsertWithIdentityAsync(item);
            return id;
        }

        public async Task<PaymentWebhookEventDto?> GetWebhookEventAsync(PaymentProvider provider, string providerEventId)
        {
            var result = await _connection.PaymentWebhookEvents
                .FirstOrDefaultAsync(i => i.Provider == provider && i.ProviderEventId == providerEventId);
            return result;
        }

        public async Task<bool> ExistsWebhookEventAsync(PaymentProvider provider, string providerEventId)
        {
            return await _connection.PaymentWebhookEvents
                .AnyAsync(i => i.Provider == provider && i.ProviderEventId == providerEventId);
        }

        public async Task MarkWebhookProcessedAsync(Guid webhookEventId, Guid? orderId = null)
        {
            var query = _connection.PaymentWebhookEvents.Where(i => i.Id == webhookEventId)
                .Set(i => i.ProcessedAt, DateTimeOffset.Now.ToUniversalTime());

            if (orderId != null)
                query = query.Set(i => i.OrderId, orderId);

            await query.UpdateAsync();
        }

        public async Task<List<PaymentWebhookEventDto>> GetUnprocessedWebhookEventsAsync(int limit = 100)
        {
            var result = await _connection.PaymentWebhookEvents
                .Where(i => i.ProcessedAt == null)
                .OrderBy(i => i.ReceivedAt)
                .Take(limit)
                .ToListAsync();
            return result;
        }
        #endregion
    }
}
