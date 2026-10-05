using EList.Common.CorrelationId;
using EList.Common.HttpRestClient;
using EList.Common.Logger;
using EList.Services.Interfaces;
using NLog;

namespace EList.Services.Impl.Payments.TBank
{
    /// <summary>
    /// Клиент SM-Register Т-Банка: auth → token, register → ShopCode.
    /// Транспорт — <see cref="HttpRestClient2"/>.
    /// </summary>
    public class TBankSmRegisterClient
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.Payments.TBank.TBankSmRegisterClient.";
        #endregion

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly TBankPaymentSettings _settings;

        public TBankSmRegisterClient(ICorrelationIdProvider correlationIdProvider, TBankPaymentSettings? settings = null)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _settings = settings ?? TBankPaymentSettings.Load();
        }

        public async Task<string> AuthorizeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureConfigured();

            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(AuthorizeAsync)}";

            var http = new HttpRestClient2(correlationId, _settings.SmRegister.BaseUrl.TrimEnd('/'));
            var response = await http.PostAsync<TBankSmRegisterAuthResponse>(
                "register/auth",
                new
                {
                    username = _settings.SmRegister.Username,
                    password = _settings.SmRegister.Password
                },
                DefaultTimeout);

            var token = response?.ResolveToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException(
                    $"SM-Register auth не вернул token: {response?.ErrorMessage ?? response?.Message ?? "empty"}");
            }

            logger.Debug(correlationId, null, methodName, "SM-Register auth OK", null);
            return token.Trim();
        }

        public async Task<TBankSmRegisterShopResponse> RegisterShopAsync(
            string bearerToken,
            SellerOnboardingRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(bearerToken))
                throw new ArgumentException("bearerToken is required", nameof(bearerToken));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            EnsureConfigured();

            var billingDescriptor = Truncate(SanitizeBillingDescriptor(request.LegalName ?? request.OrganizationId.ToString("N")), 14);
            var payload = new Dictionary<string, object?>
            {
                ["billingDescriptor"] = billingDescriptor,
                ["fullName"] = request.LegalName ?? billingDescriptor,
                ["name"] = Truncate(request.LegalName ?? billingDescriptor, 100),
                ["inn"] = DigitsOnly(request.Inn),
                ["ogrn"] = DigitsOnly(request.Ogrn),
                ["email"] = request.Email,
                ["siteUrl"] = request.ReturnUrl ?? "https://tvoy-spot.ru",
                ["addresses"] = new[]
                {
                    new Dictionary<string, object?>
                    {
                        ["type"] = "legal",
                        ["zip"] = "000000",
                        ["country"] = "RUS",
                        ["city"] = "Moscow",
                        ["street"] = request.LegalAddress ?? "не указан",
                        ["description"] = request.LegalAddress ?? "не указан"
                    }
                },
                ["ceo"] = new Dictionary<string, object?>
                {
                    ["firstName"] = SplitName(request.HeadName).First,
                    ["lastName"] = SplitName(request.HeadName).Last,
                    ["middleName"] = SplitName(request.HeadName).Middle,
                    ["phone"] = DigitsOnly(request.Phone) ?? "79000000000",
                    ["country"] = "RUS"
                },
                ["bankAccount"] = new Dictionary<string, object?>
                {
                    ["account"] = DigitsOnly(request.BankAccount),
                    ["korAccount"] = "30101810100000000000",
                    ["bik"] = DigitsOnly(request.Bik),
                    ["bankName"] = request.BankName ?? "Банк",
                    ["details"] = request.BankName ?? "Банк"
                }
            };

            var correlationId = _correlationIdProvider.Get();
            var http = new HttpRestClient2(
                correlationId,
                _settings.SmRegister.BaseUrl.TrimEnd('/'),
                $"Bearer {bearerToken.Trim()}");

            var response = await http.PostAsync<TBankSmRegisterShopResponse>("register", payload, DefaultTimeout);
            if (response == null)
                throw new InvalidOperationException("SM-Register register: пустой ответ");

            return response;
        }

        private void EnsureConfigured()
        {
            if (!_settings.SmRegister.IsConfigured())
            {
                throw new InvalidOperationException(
                    "SM-Register не настроен: задайте payments__tbank__smRegister__username и payments__tbank__smRegister__password.");
            }
        }

        private static string SanitizeBillingDescriptor(string value)
        {
            var chars = value.Where(ch => char.IsLetterOrDigit(ch) || ch == ' ').ToArray();
            var cleaned = new string(chars).Trim();
            return string.IsNullOrWhiteSpace(cleaned) ? "ELIST" : cleaned;
        }

        private static string Truncate(string? value, int max)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Length <= max ? value : value[..max];
        }

        private static string? DigitsOnly(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            var digits = new string(value.Where(char.IsDigit).ToArray());
            return string.IsNullOrEmpty(digits) ? null : digits;
        }

        private static (string Last, string First, string Middle) SplitName(string? fullName)
        {
            var parts = (fullName ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                return ("Неизвестно", "Неизвестно", string.Empty);
            if (parts.Length == 1)
                return (parts[0], parts[0], string.Empty);
            if (parts.Length == 2)
                return (parts[0], parts[1], string.Empty);
            return (parts[0], parts[1], string.Join(' ', parts.Skip(2)));
        }
    }
}
