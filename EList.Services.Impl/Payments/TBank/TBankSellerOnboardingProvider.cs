using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Models.Enums;
using EList.Services.Interfaces;
using NLog;

namespace EList.Services.Impl.Payments.TBank
{
    /// <summary>
    /// Онбординг продавца в Т-Банк: SM-Register → ShopCode (ProviderSellerId).
    /// Без SM-Register credentials — явная ошибка, либо manualShopCode для DEMO.
    /// </summary>
    public class TBankSellerOnboardingProvider : ISellerOnboardingProvider
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.Payments.TBank.TBankSellerOnboardingProvider.";
        #endregion

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly TBankPaymentSettings _settings;
        private readonly TBankSmRegisterClient _smRegisterClient;

        public TBankSellerOnboardingProvider(ICorrelationIdProvider correlationIdProvider)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _settings = TBankPaymentSettings.Load();
            _smRegisterClient = new TBankSmRegisterClient(correlationIdProvider, _settings);
        }

        public PaymentProvider Kind => PaymentProvider.Tbank;

        public bool CompletesImmediately => true;

        public async Task<SellerOnboardingStartResult> StartAsync(SellerOnboardingRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.OrganizationId == Guid.Empty)
                throw new ArgumentException("OrganizationId is required", nameof(request));

            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(StartAsync)}";

            // DEMO/локальный обход: ручной ShopCode без SM-Register.
            if (!_settings.SmRegister.IsConfigured()
                && !string.IsNullOrWhiteSpace(_settings.ManualShopCode))
            {
                logger.Debug(correlationId, null, methodName,
                    $"Using manualShopCode for org {request.OrganizationId:D}", null);

                return new SellerOnboardingStartResult
                {
                    ProviderSellerId = _settings.ManualShopCode.Trim(),
                    ConfirmationUrl = null,
                    Status = ProviderOnboardingStatus.Active
                };
            }

            if (!_settings.SmRegister.IsConfigured())
            {
                throw new InvalidOperationException(
                    "Онбординг продавца Т-Банка недоступен: задайте SM-Register credentials "
                    + "(payments__tbank__smRegister__username / password) "
                    + "или payments__tbank__manualShopCode для DEMO.");
            }

            if (string.IsNullOrWhiteSpace(request.Inn))
                throw new ArgumentException("Inn is required for SM-Register", nameof(request));
            if (string.IsNullOrWhiteSpace(request.BankAccount) || string.IsNullOrWhiteSpace(request.Bik))
                throw new ArgumentException("BankAccount and Bik are required for SM-Register", nameof(request));

            var token = await _smRegisterClient.AuthorizeAsync();
            var registered = await _smRegisterClient.RegisterShopAsync(token, request);
            var shopCode = registered.ResolveShopCode();
            if (string.IsNullOrWhiteSpace(shopCode))
            {
                throw new InvalidOperationException(
                    $"SM-Register не вернул ShopCode: {registered.ErrorMessage ?? registered.Message ?? "empty"}");
            }

            var status = MapOnboardingStatus(registered.Status);
            logger.Debug(correlationId, null, methodName,
                $"Registered ShopCode for org {request.OrganizationId:D}, status={status}", null);

            return new SellerOnboardingStartResult
            {
                ProviderSellerId = shopCode.Trim(),
                ConfirmationUrl = null,
                Status = status
            };
        }

        public Task<SellerOnboardingStatusInfo> GetStatusAsync(string providerSellerId)
        {
            // SM-Register не даёт стабильного poll статуса в этой итерации —
            // статус хранится у нас в organization_payouts.
            if (string.IsNullOrWhiteSpace(providerSellerId))
            {
                return Task.FromResult(new SellerOnboardingStatusInfo
                {
                    ProviderSellerId = providerSellerId,
                    Status = ProviderOnboardingStatus.None
                });
            }

            return Task.FromResult(new SellerOnboardingStatusInfo
            {
                ProviderSellerId = providerSellerId.Trim(),
                Status = ProviderOnboardingStatus.Active
            });
        }

        private static ProviderOnboardingStatus MapOnboardingStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return ProviderOnboardingStatus.Active;

            return status.Trim().ToUpperInvariant() switch
            {
                "ACTIVE" or "REGISTERED" or "OK" or "SUCCESS" => ProviderOnboardingStatus.Active,
                "PENDING" or "MODERATION" or "CHECKING" => ProviderOnboardingStatus.Pending,
                "REJECTED" or "FAILED" or "ERROR" => ProviderOnboardingStatus.Rejected,
                _ => ProviderOnboardingStatus.Pending
            };
        }
    }
}
