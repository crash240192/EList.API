using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Models.Enums;
using EList.Services.Interfaces;
using NLog;

namespace EList.Services.Impl.Payments.TBank
{
    /// <summary>
    /// Онбординг продавца в Т‑Банк Мультирасчёты:
    /// приоритет acqapi (OAuth + /sm-register/register) → legacy SM-Register → manualShopCode (DEMO).
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
        private readonly TBankAcqApiClient _acqApiClient;
        private readonly TBankSmRegisterClient _smRegisterClient;

        public TBankSellerOnboardingProvider(ICorrelationIdProvider correlationIdProvider)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _settings = TBankPaymentSettings.Load();
            _acqApiClient = new TBankAcqApiClient(correlationIdProvider, _settings);
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

            // DEMO/локальный обход: ручной ShopCode без bank API.
            if (!_settings.AcqApi.IsConfigured()
                && !_settings.SmRegister.IsConfigured()
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

            if (_settings.AcqApi.IsConfigured())
                return await StartViaAcqApiAsync(request, correlationId, methodName);

            if (_settings.SmRegister.IsConfigured())
                return await StartViaLegacySmRegisterAsync(request, correlationId, methodName);

            throw new InvalidOperationException(
                "Онбординг продавца Т‑Банка недоступен: задайте acqapi credentials "
                + "(payments__tbank__acqApi__username / password), "
                + "legacy SM-Register (payments__tbank__smRegister__username / password) "
                + "или payments__tbank__manualShopCode для DEMO.");
        }

        public async Task<SellerOnboardingStatusInfo> GetStatusAsync(string providerSellerId)
        {
            if (string.IsNullOrWhiteSpace(providerSellerId))
            {
                return new SellerOnboardingStatusInfo
                {
                    ProviderSellerId = providerSellerId,
                    Status = ProviderOnboardingStatus.None
                };
            }

            var shopCode = providerSellerId.Trim();

            // Опциональный live-check через acqapi GET shop; при сбое — Active по записи у нас.
            if (_settings.AcqApi.IsConfigured())
            {
                try
                {
                    var token = await _acqApiClient.AuthorizeAsync();
                    var info = await _acqApiClient.GetShopAsync(token, shopCode);
                    if (info != null)
                    {
                        return new SellerOnboardingStatusInfo
                        {
                            ProviderSellerId = shopCode,
                            Status = ProviderOnboardingStatus.Active
                        };
                    }
                }
                catch
                {
                    // статус остаётся локальным
                }
            }

            return new SellerOnboardingStatusInfo
            {
                ProviderSellerId = shopCode,
                Status = ProviderOnboardingStatus.Active
            };
        }

        private async Task<SellerOnboardingStartResult> StartViaAcqApiAsync(
            SellerOnboardingRequest request,
            string correlationId,
            string methodName)
        {
            EnsureBankPayload(request, "acqapi");

            var token = await _acqApiClient.AuthorizeAsync();
            var registered = await _acqApiClient.RegisterShopAsync(token, request);
            var shopCode = registered.ResolveShopCode();
            if (string.IsNullOrWhiteSpace(shopCode))
            {
                throw new InvalidOperationException(
                    $"acqapi register не вернул shopCode: {registered.Message ?? "empty"}");
            }

            var status = MapOnboardingStatus(registered.Status);
            logger.Debug(correlationId, null, methodName,
                $"acqapi registered shopCode for org {request.OrganizationId:D}, status={status}", null);

            return new SellerOnboardingStartResult
            {
                ProviderSellerId = shopCode,
                ConfirmationUrl = null,
                Status = status
            };
        }

        private async Task<SellerOnboardingStartResult> StartViaLegacySmRegisterAsync(
            SellerOnboardingRequest request,
            string correlationId,
            string methodName)
        {
            EnsureBankPayload(request, "SM-Register");

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
                $"Legacy SM-Register ShopCode for org {request.OrganizationId:D}, status={status}", null);

            return new SellerOnboardingStartResult
            {
                ProviderSellerId = shopCode.Trim(),
                ConfirmationUrl = null,
                Status = status
            };
        }

        private static void EnsureBankPayload(SellerOnboardingRequest request, string apiName)
        {
            if (string.IsNullOrWhiteSpace(request.Inn))
                throw new ArgumentException($"Inn is required for {apiName}", nameof(request));
            if (string.IsNullOrWhiteSpace(request.BankAccount) || string.IsNullOrWhiteSpace(request.Bik))
                throw new ArgumentException($"BankAccount and Bik are required for {apiName}", nameof(request));
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
