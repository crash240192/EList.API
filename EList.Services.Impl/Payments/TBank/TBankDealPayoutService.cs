using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Models.Enums;
using EList.Models.Orders;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using NLog;

namespace EList.Services.Impl.Payments.TBank
{
    /// <summary>
    /// P3/P4: A2C продавцу + sync Initiated + retry + closeSpDeal по TTL сделки.
    /// </summary>
    public class TBankDealPayoutService : IOrderDealPayoutService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.Payments.TBank.TBankDealPayoutService.";
        #endregion

        private readonly IOrdersRepository _ordersRepository;
        private readonly IOrganizationsRepository _organizationsRepository;
        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly TBankPaymentSettings _settings;
        private readonly TBankA2cClient _a2cClient;
        private readonly TBankAcquiringClient _acquiringClient;

        public TBankDealPayoutService(
            IOrdersRepository ordersRepository,
            IOrganizationsRepository organizationsRepository,
            ICorrelationIdProvider correlationIdProvider)
        {
            _ordersRepository = ordersRepository ?? throw new ArgumentNullException(nameof(ordersRepository));
            _organizationsRepository = organizationsRepository ?? throw new ArgumentNullException(nameof(organizationsRepository));
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _settings = TBankPaymentSettings.Load();
            _a2cClient = new TBankA2cClient(correlationIdProvider, _settings);
            _acquiringClient = new TBankAcquiringClient(correlationIdProvider, _settings);
        }

        public async Task TryPayoutSellerAfterPaidAsync(Guid orderId)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(TryPayoutSellerAfterPaidAsync)}";

            try
            {
                if (!_settings.AutoPayoutOnPaid)
                {
                    logger.Debug(correlationId, null, methodName,
                        $"Skip payout order={orderId:D}: autoPayoutOnPaid=false", null);
                    return;
                }

                var order = await _ordersRepository.GetOrderAsync(orderId);
                if (order == null)
                    return;

                if (order.Provider != PaymentProvider.Tbank)
                    return;

                if (order.AmountTotal <= 0 || order.AmountSeller <= 0)
                    return;

                if (order.ProviderDealClosedAt != null)
                {
                    logger.Debug(correlationId, null, methodName,
                        $"Skip payout order={orderId:D}: deal already closed", null);
                    return;
                }

                if (string.IsNullOrWhiteSpace(order.ProviderDealId))
                {
                    await UpsertSkippedAsync(order, partnerId: null,
                        "Нет provider_deal_id — A2C без DealId невозможен");
                    return;
                }

                var existing = await _ordersRepository.GetOrderPayoutAsync(order.Id, OrderPayoutKind.Seller);
                if (existing != null
                    && existing.Status is OrderPayoutStatus.Succeeded or OrderPayoutStatus.Initiated)
                {
                    logger.Debug(correlationId, null, methodName,
                        $"Skip payout order={orderId:D}: already {existing.Status}", null);
                    return;
                }

                var orgPayout = await _organizationsRepository.GetPayoutAsync(order.SellerOrganizationId);
                var partnerId = SellerPayoutGate.ResolveShopCode(orgPayout, _settings);
                if (string.IsNullOrWhiteSpace(partnerId))
                {
                    await UpsertFailedOrSkippedAsync(existing, order, partnerId: null,
                        OrderPayoutStatus.Skipped,
                        "Нет ShopCode (PartnerId) у продавца");
                    return;
                }

                try
                {
                    _settings.EnsureAcquiringConfigured();
                }
                catch (InvalidOperationException ex)
                {
                    await UpsertFailedOrSkippedAsync(existing, order, partnerId,
                        OrderPayoutStatus.Skipped, ex.Message);
                    return;
                }

                var payout = existing ?? new OrderPayout
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    Kind = OrderPayoutKind.Seller,
                    Amount = order.AmountSeller,
                    Currency = string.IsNullOrWhiteSpace(order.Currency) ? "RUB" : order.Currency,
                    Provider = PaymentProvider.Tbank,
                    ProviderDealId = order.ProviderDealId,
                    PartnerId = partnerId,
                    FinalPayout = true,
                    Status = OrderPayoutStatus.Pending,
                    CreateDate = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                if (existing == null)
                {
                    payout.Id = await _ordersRepository.CreateOrderPayoutAsync(payout);
                }
                else
                {
                    payout.PartnerId = partnerId;
                    payout.ProviderDealId = order.ProviderDealId;
                    payout.Amount = order.AmountSeller;
                    payout.FinalPayout = true;
                    payout.Status = OrderPayoutStatus.Pending;
                    payout.ErrorMessage = null;
                    payout.ProviderPaymentId = null;
                    payout.ProviderOrderId = null;
                    payout.CompletedAt = null;
                    await _ordersRepository.UpdateOrderPayoutAsync(payout);
                }

                var providerOrderId = BuildProviderOrderId(payout.Id);
                var recipientId = BuildPaymentRecipientId(order.SellerOrganizationId, partnerId);
                object dealIdValue = long.TryParse(order.ProviderDealId.Trim(), out var dealNum)
                    ? dealNum
                    : order.ProviderDealId.Trim();

                var init = await _a2cClient.InitAsync(new TBankA2cInitRequest
                {
                    TerminalKey = _settings.TerminalKey,
                    OrderId = providerOrderId,
                    PaymentRecipientId = recipientId,
                    DealId = dealIdValue,
                    PartnerId = partnerId,
                    Amount = TBankAcquiringClient.ToKopecks(order.AmountSeller),
                    FinalPayout = true
                });

                if (string.IsNullOrWhiteSpace(init.PaymentId))
                    throw new InvalidOperationException("e2c Init не вернул PaymentId");

                payout.ProviderOrderId = providerOrderId;
                payout.ProviderPaymentId = init.PaymentId.Trim();
                payout.Status = OrderPayoutStatus.Initiated;
                payout.UpdatedAt = DateTimeOffset.UtcNow;
                await _ordersRepository.UpdateOrderPayoutAsync(payout);

                var payment = await _a2cClient.PaymentAsync(payout.ProviderPaymentId);
                var status = (payment.Status ?? init.Status ?? string.Empty).Trim().ToUpperInvariant();
                await ApplyPayoutTerminalStatusAsync(payout, status, order.Id);

                logger.Debug(correlationId, null, methodName,
                    $"Payout order={orderId:D} status={payout.Status} payment={payout.ProviderPaymentId}", null);
            }
            catch (Exception ex)
            {
                logger.Error(correlationId, null, methodName,
                    $"Payout failed order={orderId:D}: {ex.Message}", null, ex);
                try
                {
                    var order = await _ordersRepository.GetOrderAsync(orderId);
                    if (order == null)
                        return;
                    var existing = await _ordersRepository.GetOrderPayoutAsync(orderId, OrderPayoutKind.Seller);
                    await UpsertFailedOrSkippedAsync(existing, order, partnerId: existing?.PartnerId,
                        OrderPayoutStatus.Failed, Truncate(ex.Message, 500));
                }
                catch
                {
                    // ignore secondary failures
                }
            }
        }

        public async Task<int> SyncInitiatedPayoutsAsync(int limit = 50)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(SyncInitiatedPayoutsAsync)}";
            if (limit <= 0)
                limit = 50;

            var initiated = await _ordersRepository.GetOrderPayoutsByStatusAsync(
                OrderPayoutStatus.Initiated, limit);
            var updated = 0;

            foreach (var payout in initiated)
            {
                if (string.IsNullOrWhiteSpace(payout.ProviderPaymentId))
                    continue;

                try
                {
                    _settings.EnsureAcquiringConfigured();
                    var state = await _a2cClient.GetStateAsync(payout.ProviderPaymentId);
                    var status = (state.Status ?? string.Empty).Trim().ToUpperInvariant();
                    var before = payout.Status;
                    await ApplyPayoutTerminalStatusAsync(payout, status, payout.OrderId);
                    if (payout.Status != before)
                        updated++;
                }
                catch (Exception ex)
                {
                    logger.Warn(correlationId, null, methodName,
                        $"GetState payout={payout.Id}: {ex.Message}", null);
                }
            }

            if (updated > 0)
            {
                logger.Info(correlationId, null, methodName,
                    $"Synced initiated payouts updated={updated}/{initiated.Count}", null);
            }

            return updated;
        }

        public async Task<int> RetryOpenDealPayoutsAsync(int limit = 50)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(RetryOpenDealPayoutsAsync)}";
            if (limit <= 0)
                limit = 50;

            // Ранний ретрай Failed: через 15 минут; общий open-deal — с половины deal term.
            var retryAfter = TimeSpan.FromDays(Math.Max(1, _settings.DealTermDays) / 2.0);
            if (retryAfter < TimeSpan.FromHours(1))
                retryAfter = TimeSpan.FromHours(1);

            var paidBefore = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(15));
            var orders = await _ordersRepository.GetPaidOrdersWithOpenDealAsync(paidBefore, limit);
            var retried = 0;

            foreach (var order in orders)
            {
                var existing = await _ordersRepository.GetOrderPayoutAsync(order.Id, OrderPayoutKind.Seller);
                if (existing?.Status == OrderPayoutStatus.Succeeded)
                    continue;
                if (existing?.Status == OrderPayoutStatus.Initiated)
                    continue; // SyncInitiated разберёт

                if (existing?.Status == OrderPayoutStatus.Failed)
                {
                    if (existing.UpdatedAt > DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(15)))
                        continue;
                }
                else if (existing?.Status == OrderPayoutStatus.Skipped
                         || existing == null)
                {
                    var paidAt = order.PaidAt ?? order.CreateDate;
                    if (paidAt > DateTimeOffset.UtcNow.Subtract(retryAfter))
                        continue;
                }

                await TryPayoutSellerAfterPaidAsync(order.Id);
                retried++;
            }

            if (retried > 0)
            {
                logger.Info(correlationId, null, methodName,
                    $"Retry payouts started={retried}", null);
            }

            return retried;
        }

        public async Task<int> CloseExpiredOpenDealsAsync(int limit = 50)
        {
            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(CloseExpiredOpenDealsAsync)}";
            if (limit <= 0)
                limit = 50;

            var term = TimeSpan.FromDays(Math.Max(1, _settings.DealTermDays));
            var paidBefore = DateTimeOffset.UtcNow.Subtract(term);
            var orders = await _ordersRepository.GetPaidOrdersWithOpenDealAsync(paidBefore, limit);
            var closed = 0;

            foreach (var order in orders)
            {
                if (string.IsNullOrWhiteSpace(order.ProviderDealId))
                    continue;

                // Последняя попытка payout перед close.
                await TryPayoutSellerAfterPaidAsync(order.Id);
                var refreshed = await _ordersRepository.GetOrderAsync(order.Id);
                if (refreshed?.ProviderDealClosedAt != null)
                {
                    closed++;
                    continue;
                }

                var seller = await _ordersRepository.GetOrderPayoutAsync(order.Id, OrderPayoutKind.Seller);
                if (seller?.Status == OrderPayoutStatus.Succeeded && seller.FinalPayout)
                {
                    await MarkDealClosedAsync(order.Id);
                    closed++;
                    continue;
                }

                if (seller?.Status == OrderPayoutStatus.Initiated)
                    continue; // ждём GetState

                try
                {
                    _settings.EnsureAcquiringConfigured();
                    await _acquiringClient.CloseSpDealAsync(order.ProviderDealId);
                    await MarkDealClosedAsync(order.Id);
                    closed++;
                    logger.Info(correlationId, null, methodName,
                        $"closeSpDeal OK order={order.Id:D} deal={order.ProviderDealId}", null);
                }
                catch (Exception ex)
                {
                    logger.Warn(correlationId, null, methodName,
                        $"closeSpDeal failed order={order.Id:D}: {ex.Message}", null);
                }
            }

            if (closed > 0)
            {
                logger.Info(correlationId, null, methodName,
                    $"Closed deals={closed}", null);
            }

            return closed;
        }

        private async Task ApplyPayoutTerminalStatusAsync(OrderPayout payout, string status, Guid orderId)
        {
            if (status is "REJECTED" or "CANCELED" or "CANCELLED" or "DEADLINE_EXPIRED" or "AUTH_FAIL")
            {
                payout.Status = OrderPayoutStatus.Failed;
                payout.ErrorMessage = $"e2c status={status}";
                payout.CompletedAt = DateTimeOffset.UtcNow;
            }
            else if (status is "COMPLETED" or "CONFIRMED" or "SUCCESS" or "CHECKED" or "")
            {
                payout.Status = OrderPayoutStatus.Succeeded;
                payout.CompletedAt = DateTimeOffset.UtcNow;
                payout.ErrorMessage = null;
            }
            else
            {
                payout.Status = OrderPayoutStatus.Initiated;
                payout.ErrorMessage = $"e2c in progress status={status}";
            }

            payout.UpdatedAt = DateTimeOffset.UtcNow;
            await _ordersRepository.UpdateOrderPayoutAsync(payout);

            if (payout.Status == OrderPayoutStatus.Succeeded && payout.FinalPayout)
                await MarkDealClosedAsync(orderId);
        }

        private async Task MarkDealClosedAsync(Guid orderId)
        {
            await _ordersRepository.SetProviderDealClosedAtAsync(orderId, DateTimeOffset.UtcNow);
        }

        private async Task UpsertSkippedAsync(Order order, string? partnerId, string reason)
            => await UpsertFailedOrSkippedAsync(null, order, partnerId, OrderPayoutStatus.Skipped, reason);

        private async Task UpsertFailedOrSkippedAsync(
            OrderPayout? existing,
            Order order,
            string? partnerId,
            OrderPayoutStatus status,
            string reason)
        {
            if (existing != null && existing.Status == OrderPayoutStatus.Succeeded)
                return;

            if (existing == null)
            {
                await _ordersRepository.CreateOrderPayoutAsync(new OrderPayout
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    Kind = OrderPayoutKind.Seller,
                    Amount = order.AmountSeller,
                    Currency = string.IsNullOrWhiteSpace(order.Currency) ? "RUB" : order.Currency,
                    Provider = PaymentProvider.Tbank,
                    ProviderDealId = order.ProviderDealId,
                    PartnerId = partnerId,
                    FinalPayout = true,
                    Status = status,
                    ErrorMessage = Truncate(reason, 500),
                    CreateDate = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    CompletedAt = status is OrderPayoutStatus.Skipped or OrderPayoutStatus.Failed
                        ? DateTimeOffset.UtcNow
                        : null
                });
                return;
            }

            existing.Status = status;
            existing.ErrorMessage = Truncate(reason, 500);
            existing.PartnerId = partnerId ?? existing.PartnerId;
            existing.ProviderDealId = order.ProviderDealId ?? existing.ProviderDealId;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            existing.CompletedAt = DateTimeOffset.UtcNow;
            await _ordersRepository.UpdateOrderPayoutAsync(existing);
        }

        private static string BuildProviderOrderId(Guid payoutId)
            => $"po:{payoutId:N}"[..Math.Min(50, $"po:{payoutId:N}".Length)];

        private static string BuildPaymentRecipientId(Guid sellerOrganizationId, string partnerId)
        {
            var digits = new string(partnerId.Where(char.IsDigit).ToArray());
            if (digits.Length is > 0 and <= 16)
                return digits;
            return sellerOrganizationId.ToString("N")[..16];
        }

        private static string Truncate(string? value, int max)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Length <= max ? value : value[..max];
        }
    }

    public class NoOpOrderDealPayoutService : IOrderDealPayoutService
    {
        public Task TryPayoutSellerAfterPaidAsync(Guid orderId) => Task.CompletedTask;

        public Task<int> SyncInitiatedPayoutsAsync(int limit = 50) => Task.FromResult(0);

        public Task<int> RetryOpenDealPayoutsAsync(int limit = 50) => Task.FromResult(0);

        public Task<int> CloseExpiredOpenDealsAsync(int limit = 50) => Task.FromResult(0);
    }
}
