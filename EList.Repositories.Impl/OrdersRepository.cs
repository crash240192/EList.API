using AutoMapper;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.Enums;
using EList.Models.Orders;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class OrdersRepository : IOrdersRepository
    {
        private readonly IOrdersDataProvider _ordersDataProvider;
        private readonly IMapper _mapper;

        public OrdersRepository(IOrdersDataProvider ordersDataProvider,
            IMapper mapper)
        {
            _ordersDataProvider = ordersDataProvider;
            _mapper = mapper;
        }

        #region orders
        public async Task<Guid> CreateOrderAsync(Order item)
        {
            var mappedItem = _mapper.Map<OrderDto>(item);
            var result = await _ordersDataProvider.CreateOrderAsync(mappedItem);
            return result;
        }

        public async Task<Order?> GetOrderAsync(Guid id)
        {
            var item = await _ordersDataProvider.GetOrderAsync(id);
            var result = _mapper.Map<Order>(item);
            return result;
        }

        public async Task<Order?> GetOrderFullAsync(Guid id)
        {
            var item = await _ordersDataProvider.GetOrderFullAsync(id);
            var result = _mapper.Map<Order>(item);
            return result;
        }

        public async Task<Order?> GetOrderByIdempotencyKeyAsync(string idempotencyKey)
        {
            var item = await _ordersDataProvider.GetOrderByIdempotencyKeyAsync(idempotencyKey);
            var result = _mapper.Map<Order>(item);
            return result;
        }

        public async Task<Order?> GetOrderByProviderPaymentAsync(PaymentProvider provider, string providerPaymentId)
        {
            var mappedProvider = _mapper.Map<DbDataProvider.Models.Enums.PaymentProvider>(provider);
            var item = await _ordersDataProvider.GetOrderByProviderPaymentAsync(mappedProvider, providerPaymentId);
            var result = _mapper.Map<Order>(item);
            return result;
        }

        public async Task UpdateOrderStatusAsync(Guid orderId, OrderStatus status)
        {
            var mappedStatus = _mapper.Map<DbDataProvider.Models.Enums.OrderStatus>(status);
            await _ordersDataProvider.UpdateOrderStatusAsync(orderId, mappedStatus);
        }

        public async Task SetOrderPaidAsync(Guid orderId, DateTimeOffset paidAt)
        {
            await _ordersDataProvider.SetOrderPaidAsync(orderId, paidAt);
        }

        public async Task SetProviderPaymentAsync(Guid orderId, PaymentProvider provider, string providerPaymentId)
        {
            var mappedProvider = _mapper.Map<DbDataProvider.Models.Enums.PaymentProvider>(provider);
            await _ordersDataProvider.SetProviderPaymentAsync(orderId, mappedProvider, providerPaymentId);
        }

        public async Task<List<Order>> GetOrdersByBuyerAsync(Guid buyerAccountId)
        {
            var items = await _ordersDataProvider.GetOrdersByBuyerAsync(buyerAccountId);
            var result = _mapper.Map<List<Order>>(items);
            return result;
        }

        public async Task<List<Order>> GetOrdersBySellerOrganizationAsync(Guid organizationId)
        {
            var items = await _ordersDataProvider.GetOrdersBySellerOrganizationAsync(organizationId);
            var result = _mapper.Map<List<Order>>(items);
            return result;
        }

        public async Task<List<Order>> GetOrdersByEventAsync(Guid eventId)
        {
            var items = await _ordersDataProvider.GetOrdersByEventAsync(eventId);
            var result = _mapper.Map<List<Order>>(items);
            return result;
        }
        #endregion

        #region tickets
        public async Task<Guid> CreateTicketAsync(Ticket item)
        {
            var mappedItem = _mapper.Map<TicketDto>(item);
            var result = await _ordersDataProvider.CreateTicketAsync(mappedItem);
            return result;
        }

        public async Task CreateTicketsAsync(List<Ticket> items)
        {
            var mappedItems = _mapper.Map<List<TicketDto>>(items);
            await _ordersDataProvider.CreateTicketsAsync(mappedItems);
        }

        public async Task<Ticket?> GetTicketAsync(Guid id)
        {
            var item = await _ordersDataProvider.GetTicketAsync(id);
            var result = _mapper.Map<Ticket>(item);
            return result;
        }

        public async Task<Ticket?> GetTicketByCodeAsync(string code)
        {
            var item = await _ordersDataProvider.GetTicketByCodeAsync(code);
            var result = _mapper.Map<Ticket>(item);
            return result;
        }

        public async Task<List<Ticket>> GetTicketsByOrderAsync(Guid orderId)
        {
            var items = await _ordersDataProvider.GetTicketsByOrderAsync(orderId);
            var result = _mapper.Map<List<Ticket>>(items);
            return result;
        }

        public async Task<List<Ticket>> GetTicketsByHolderAsync(Guid holderAccountId)
        {
            var items = await _ordersDataProvider.GetTicketsByHolderAsync(holderAccountId);
            var result = _mapper.Map<List<Ticket>>(items);
            return result;
        }

        public async Task<List<Ticket>> GetTicketsByEventAsync(Guid eventId)
        {
            var items = await _ordersDataProvider.GetTicketsByEventAsync(eventId);
            var result = _mapper.Map<List<Ticket>>(items);
            return result;
        }

        public async Task UpdateTicketStatusAsync(Guid ticketId, TicketStatus status)
        {
            var mappedStatus = _mapper.Map<DbDataProvider.Models.Enums.TicketStatus>(status);
            await _ordersDataProvider.UpdateTicketStatusAsync(ticketId, mappedStatus);
        }

        public async Task UpdateTicketsStatusByOrderAsync(Guid orderId, TicketStatus status)
        {
            var mappedStatus = _mapper.Map<DbDataProvider.Models.Enums.TicketStatus>(status);
            await _ordersDataProvider.UpdateTicketsStatusByOrderAsync(orderId, mappedStatus);
        }
        #endregion

        #region refunds
        public async Task<Guid> CreateRefundAsync(Refund item)
        {
            var mappedItem = _mapper.Map<RefundDto>(item);
            var result = await _ordersDataProvider.CreateRefundAsync(mappedItem);
            return result;
        }

        public async Task<Refund?> GetRefundAsync(Guid id)
        {
            var item = await _ordersDataProvider.GetRefundAsync(id);
            var result = _mapper.Map<Refund>(item);
            return result;
        }

        public async Task<Refund?> GetRefundByProviderRefundIdAsync(string providerRefundId)
        {
            var item = await _ordersDataProvider.GetRefundByProviderRefundIdAsync(providerRefundId);
            var result = _mapper.Map<Refund>(item);
            return result;
        }

        public async Task<List<Refund>> GetRefundsByOrderAsync(Guid orderId)
        {
            var items = await _ordersDataProvider.GetRefundsByOrderAsync(orderId);
            var result = _mapper.Map<List<Refund>>(items);
            return result;
        }

        public async Task UpdateRefundStatusAsync(Guid refundId, RefundStatus status, string? providerRefundId = null)
        {
            var mappedStatus = _mapper.Map<DbDataProvider.Models.Enums.RefundStatus>(status);
            await _ordersDataProvider.UpdateRefundStatusAsync(refundId, mappedStatus, providerRefundId);
        }
        #endregion

        #region webhooks
        public async Task<Guid> CreateWebhookEventAsync(PaymentWebhookEvent item)
        {
            var mappedItem = _mapper.Map<PaymentWebhookEventDto>(item);
            var result = await _ordersDataProvider.CreateWebhookEventAsync(mappedItem);
            return result;
        }

        public async Task<PaymentWebhookEvent?> GetWebhookEventAsync(PaymentProvider provider, string providerEventId)
        {
            var mappedProvider = _mapper.Map<DbDataProvider.Models.Enums.PaymentProvider>(provider);
            var item = await _ordersDataProvider.GetWebhookEventAsync(mappedProvider, providerEventId);
            var result = _mapper.Map<PaymentWebhookEvent>(item);
            return result;
        }

        public async Task<bool> ExistsWebhookEventAsync(PaymentProvider provider, string providerEventId)
        {
            var mappedProvider = _mapper.Map<DbDataProvider.Models.Enums.PaymentProvider>(provider);
            return await _ordersDataProvider.ExistsWebhookEventAsync(mappedProvider, providerEventId);
        }

        public async Task MarkWebhookProcessedAsync(Guid webhookEventId, Guid? orderId = null)
        {
            await _ordersDataProvider.MarkWebhookProcessedAsync(webhookEventId, orderId);
        }

        public async Task<List<PaymentWebhookEvent>> GetUnprocessedWebhookEventsAsync(int limit = 100)
        {
            var items = await _ordersDataProvider.GetUnprocessedWebhookEventsAsync(limit);
            var result = _mapper.Map<List<PaymentWebhookEvent>>(items);
            return result;
        }
        #endregion
    }
}
