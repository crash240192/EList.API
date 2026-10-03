using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Models.Enums;
using EList.Services.Interfaces;
using NLog;

namespace EList.Services.Impl.Payments.TBank
{
    /// <summary>
    /// Реальный эквайринг Т-Банка с мультирасчётами (Init + Shops/Fee).
    /// </summary>
    public class TBankPaymentProvider : IPaymentProvider
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.Payments.TBank.TBankPaymentProvider.";
        #endregion

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly TBankAcquiringClient _client;
        private readonly TBankPaymentSettings _settings;

        public TBankPaymentProvider(ICorrelationIdProvider correlationIdProvider)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _settings = TBankPaymentSettings.Load();
            _client = new TBankAcquiringClient(correlationIdProvider, _settings);
        }

        public PaymentProvider Kind => PaymentProvider.Tbank;

        public bool SupportsManualComplete => false;

        public async Task<PaymentCreationResult> CreatePaymentAsync(PaymentCreationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.Amount < 0)
                throw new ArgumentOutOfRangeException(nameof(request.Amount));

            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(CreatePaymentAsync)}";

            _settings.EnsureAcquiringConfigured();

            var amountKopecks = TBankAcquiringClient.ToKopecks(request.Amount);
            var orderId = request.WalletDepositId != null && request.WalletDepositId != Guid.Empty
                ? $"wallet:{request.WalletDepositId:D}"
                : request.OrderId.ToString("D");

            var successUrl = !string.IsNullOrWhiteSpace(_settings.SuccessUrl)
                ? _settings.SuccessUrl
                : request.ReturnUrl;
            var failUrl = !string.IsNullOrWhiteSpace(_settings.FailUrl)
                ? _settings.FailUrl
                : request.ReturnUrl;

            var init = new TBankInitRequest
            {
                TerminalKey = _settings.TerminalKey,
                Amount = amountKopecks,
                OrderId = orderId,
                Description = Truncate(request.Description, 250),
                SuccessURL = successUrl,
                FailURL = failUrl,
                NotificationURL = _settings.NotificationUrl,
                PayType = "O"
            };

            var shopCode = !string.IsNullOrWhiteSpace(request.SellerShopCode)
                ? request.SellerShopCode.Trim()
                : null;

            // Билетный сплит — только при наличии ShopCode. Кошелёк/площадка — без Shops.
            if (!string.IsNullOrWhiteSpace(shopCode)
                && request.AmountSeller != null
                && request.WalletDepositId == null)
            {
                var sellerKopecks = TBankAcquiringClient.ToKopecks(request.AmountSeller.Value);
                var feeKopecks = request.AmountCommission != null
                    ? TBankAcquiringClient.ToKopecks(request.AmountCommission.Value)
                    : 0L;

                if (sellerKopecks + feeKopecks != amountKopecks && feeKopecks > 0)
                {
                    // Подгонка копеек: Fee = total - seller.
                    feeKopecks = Math.Max(0, amountKopecks - sellerKopecks);
                }

                init.Shops = new List<TBankShopDto>
                {
                    new()
                    {
                        ShopCode = shopCode,
                        Amount = sellerKopecks,
                        Name = Truncate(request.Description, 100),
                        Fee = feeKopecks > 0 ? TBankAcquiringClient.FormatKopecks(feeKopecks) : null
                    }
                };
            }

            logger.Debug(correlationId, null, methodName,
                $"Init OrderId={orderId} Amount={amountKopecks} Shops={(init.Shops != null ? "yes" : "no")}",
                null);

            var response = await _client.InitAsync(init);
            if (string.IsNullOrWhiteSpace(response.PaymentId))
                throw new InvalidOperationException("Т-Банк Init не вернул PaymentId");

            return new PaymentCreationResult
            {
                ProviderPaymentId = response.PaymentId.Trim(),
                ConfirmationUrl = response.PaymentURL,
                Status = MapStatus(response.Status)
            };
        }

        public async Task<PaymentStatusInfo> GetStatusAsync(string providerPaymentId)
        {
            if (string.IsNullOrWhiteSpace(providerPaymentId))
            {
                return new PaymentStatusInfo
                {
                    ProviderPaymentId = providerPaymentId,
                    Status = PaymentProviderStatus.Failed
                };
            }

            var response = await _client.GetStateAsync(providerPaymentId);
            return new PaymentStatusInfo
            {
                ProviderPaymentId = response.PaymentId ?? providerPaymentId,
                Status = MapStatus(response.Status),
                Amount = response.Amount != null
                    ? TBankAcquiringClient.FromKopecks(response.Amount.Value)
                    : null,
                Currency = "RUB"
            };
        }

        public Task CompleteManuallyAsync(string providerPaymentId)
        {
            throw new NotSupportedException(
                "Ручное подтверждение недоступно для Т-Банка; дождитесь webhook NotificationURL.");
        }

        public async Task<RefundCreationResult> CreateRefundAsync(RefundCreationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.ProviderPaymentId))
                throw new ArgumentException("ProviderPaymentId is required", nameof(request));
            if (request.Amount < 0)
                throw new ArgumentOutOfRangeException(nameof(request.Amount));

            var amountKopecks = TBankAcquiringClient.ToKopecks(request.Amount);
            var response = await _client.CancelAsync(request.ProviderPaymentId.Trim(), amountKopecks);

            // Cancel/Refund у Т-Банка возвращает тот же PaymentId; используем его + refund guid как ключ.
            var providerRefundId = !string.IsNullOrWhiteSpace(response.PaymentId)
                ? $"{response.PaymentId.Trim()}:rfnd:{request.RefundId:N}"
                : $"tbank_rfnd_{request.RefundId:N}";

            return new RefundCreationResult
            {
                ProviderRefundId = providerRefundId,
                Status = MapRefundStatus(response.Status)
            };
        }

        public Task CompleteRefundManuallyAsync(string providerRefundId)
        {
            throw new NotSupportedException(
                "Ручное подтверждение возврата недоступно для Т-Банка; дождитесь webhook.");
        }

        public static PaymentProviderStatus MapStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return PaymentProviderStatus.Pending;

            return status.Trim().ToUpperInvariant() switch
            {
                "CONFIRMED" => PaymentProviderStatus.Succeeded,
                "AUTHORIZED" => PaymentProviderStatus.Pending,
                "NEW" or "FORM_SHOWED" or "AUTHORIZING" or "3DS_CHECKING" or "3DS_CHECKED"
                    or "CONFIRMING" or "PREAUTHORIZING" or "PAY_CHECKING" => PaymentProviderStatus.Pending,
                "REJECTED" or "DEADLINE_EXPIRED" or "AUTH_FAIL" => PaymentProviderStatus.Failed,
                "CANCELED" or "CANCELLED" or "REVERSED" => PaymentProviderStatus.Canceled,
                "REFUNDED" or "PARTIAL_REFUNDED" or "REFUNDING" => PaymentProviderStatus.Succeeded,
                _ => PaymentProviderStatus.Pending
            };
        }

        private static PaymentProviderStatus MapRefundStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return PaymentProviderStatus.Pending;

            return status.Trim().ToUpperInvariant() switch
            {
                "REFUNDED" or "PARTIAL_REFUNDED" => PaymentProviderStatus.Succeeded,
                "REFUNDING" => PaymentProviderStatus.Pending,
                "REJECTED" => PaymentProviderStatus.Failed,
                "CANCELED" or "CANCELLED" => PaymentProviderStatus.Canceled,
                // Cancel на NEW/AUTHORIZING часто сразу CANCELED
                _ => MapStatus(status)
            };
        }

        private static string? Truncate(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;
            var trimmed = value.Trim();
            return trimmed.Length <= max ? trimmed : trimmed[..max];
        }
    }
}
