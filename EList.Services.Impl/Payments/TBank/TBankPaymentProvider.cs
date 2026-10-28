using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Models.Enums;
using EList.Services.Interfaces;
using NLog;

namespace EList.Services.Impl.Payments.TBank
{
    /// <summary>
    /// Эквайринг Т‑Банка: билеты — Мультирасчёты Deal NN (Init + CreateDealWithType);
    /// кошелёк — обычный Init без сделки. Legacy Shops/Fee больше не шлём.
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
            var orderId = !string.IsNullOrWhiteSpace(request.ClientOrderId)
                ? request.ClientOrderId.Trim()
                : request.WalletDepositId != null && request.WalletDepositId != Guid.Empty
                    ? $"wallet:{request.WalletDepositId:D}"
                    : request.OrderId.ToString("D");

            var successBase = !string.IsNullOrWhiteSpace(_settings.SuccessUrl)
                ? _settings.SuccessUrl
                : request.ReturnUrl;
            var failBase = !string.IsNullOrWhiteSpace(_settings.FailUrl)
                ? _settings.FailUrl
                : request.ReturnUrl;

            var successUrl = BuildCustomerReturnUrl(successBase, request);
            var failUrl = BuildCustomerReturnUrl(failBase, request);

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

            // Билеты с онбордингом продавца → сделка NN (одностадийка). Кошелёк — без Deal.
            var useDeal = !string.IsNullOrWhiteSpace(shopCode)
                && request.WalletDepositId == null
                && request.SellerOrganizationId != null;

            if (useDeal)
                init.CreateDealWithType = "NN";

            if (_settings.Receipt?.Enabled == true)
                init.Receipt = BuildReceipt(request, amountKopecks);

            logger.Debug(correlationId, null, methodName,
                $"Init OrderId={orderId} Amount={amountKopecks} Deal={(useDeal ? "NN" : "no")} Receipt={(_settings.Receipt?.Enabled == true)}",
                null);

            var response = await _client.InitAsync(init);
            if (string.IsNullOrWhiteSpace(response.PaymentId))
                throw new InvalidOperationException("Т-Банк Init не вернул PaymentId");

            return new PaymentCreationResult
            {
                ProviderPaymentId = response.PaymentId.Trim(),
                ConfirmationUrl = response.PaymentURL,
                Status = MapStatus(response.Status),
                ProviderDealId = response.ResolveDealId()
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
                Currency = "RUB",
                ProviderDealId = response.ResolveDealId()
            };
        }

        public Task CompleteManuallyAsync(string providerPaymentId)
        {
            throw new NotSupportedException(
                "Ручное подтверждение недоступно для Т-Банка; дождитесь webhook NotificationURL.");
        }

        public async Task CancelPaymentAsync(string providerPaymentId)
        {
            if (string.IsNullOrWhiteSpace(providerPaymentId))
                throw new ArgumentException("providerPaymentId is required", nameof(providerPaymentId));

            await _client.CancelAsync(providerPaymentId.Trim(), amountKopecks: null);
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

        /// <summary>
        /// SuccessURL / FailURL с query для return-страницы UI
        /// (как у stub: depositId+purpose=wallet или orderId).
        /// </summary>
        public static string? BuildCustomerReturnUrl(string? baseUrl, PaymentCreationRequest request)
        {
            if (string.IsNullOrWhiteSpace(baseUrl) || request == null)
                return baseUrl;

            var root = baseUrl.Trim();
            // Срезаем хвостовой '?' / '&', чтобы стабильно добавить свои параметры.
            root = root.TrimEnd('?', '&');

            string query;
            if (request.WalletDepositId != null && request.WalletDepositId != Guid.Empty)
            {
                query = $"depositId={request.WalletDepositId:D}&purpose=wallet";
            }
            else if (request.OrderId != Guid.Empty)
            {
                query = $"orderId={request.OrderId:D}";
            }
            else
            {
                return root;
            }

            var separator = root.Contains('?', StringComparison.Ordinal) ? '&' : '?';
            return $"{root}{separator}{query}";
        }

        private TBankReceiptDto BuildReceipt(PaymentCreationRequest request, long amountKopecks)
        {
            var cfg = _settings.Receipt ?? new TBankReceiptSettings();
            var email = FirstNonEmpty(request.BuyerEmail, cfg.DefaultEmail);
            var phone = NormalizePhone(request.BuyerPhone);

            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
            {
                throw new InvalidOperationException(
                    "Т-Банк Init.Receipt: нужен Email или Phone покупателя "
                    + "(login с @, BuyerPhone, или payments__tbank__receipt__defaultEmail). "
                    + "Либо отключите кассу на терминале / payments__tbank__receipt__enabled=false.");
            }

            var name = Truncate(request.Description, 128) ?? "Оплата EList";
            return new TBankReceiptDto
            {
                Email = email,
                Phone = phone,
                Taxation = string.IsNullOrWhiteSpace(cfg.Taxation) ? "usn_income" : cfg.Taxation.Trim(),
                Items = new List<TBankReceiptItemDto>
                {
                    new()
                    {
                        Name = name,
                        Price = amountKopecks,
                        Quantity = 1,
                        Amount = amountKopecks,
                        Tax = string.IsNullOrWhiteSpace(cfg.Tax) ? "none" : cfg.Tax.Trim(),
                        PaymentMethod = "full_payment",
                        PaymentObject = string.IsNullOrWhiteSpace(cfg.PaymentObject)
                            ? "service"
                            : cfg.PaymentObject.Trim()
                    }
                }
            };
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
            return null;
        }

        private static string? NormalizePhone(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            var digits = new string(value.Where(char.IsDigit).ToArray());
            if (digits.Length == 0)
                return null;
            if (digits.Length == 11 && digits.StartsWith('8'))
                digits = "7" + digits[1..];
            if (digits.StartsWith('7') && digits.Length == 11)
                return "+" + digits;
            if (digits.Length >= 10)
                return "+" + digits;
            return null;
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
