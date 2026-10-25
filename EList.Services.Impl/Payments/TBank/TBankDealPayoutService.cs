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
    /// P3: после CONFIRMED — e2c Init+Payment продавцу (PartnerId = shopCode), FinalPayout=true.
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

                if (status is "REJECTED" or "CANCELED" or "CANCELLED" or "DEADLINE_EXPIRED" or "AUTH_FAIL")
                {
                    payout.Status = OrderPayoutStatus.Failed;
                    payout.ErrorMessage = $"e2c Payment status={status}";
                }
                else if (status is "COMPLETED" or "CONFIRMED" or "SUCCESS" or "CHECKED" or "")
                {
                    // Пустой Status при Success=true — ок. Иначе COMPLETED и аналоги.
                    payout.Status = OrderPayoutStatus.Succeeded;
                    payout.CompletedAt = DateTimeOffset.UtcNow;
                    payout.ErrorMessage = null;
                }
                else
                {
                    // CREDIT_CHECKING / etc. — Initiated; GetState/worker — P4.
                    payout.Status = OrderPayoutStatus.Initiated;
                    payout.ErrorMessage = $"e2c Payment in progress status={status}";
                }

                payout.UpdatedAt = DateTimeOffset.UtcNow;
                await _ordersRepository.UpdateOrderPayoutAsync(payout);

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

        /// <summary>PaymentRecipientId ≤ 16 символов.</summary>
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

    /// <summary>No-op для stub / не-tbank провайдера.</summary>
    public class NoOpOrderDealPayoutService : IOrderDealPayoutService
    {
        public Task TryPayoutSellerAfterPaidAsync(Guid orderId) => Task.CompletedTask;
    }
}
