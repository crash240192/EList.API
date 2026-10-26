using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace EList.Services.Impl.Payments.CloudKassir
{
    /// <summary>
    /// CloudKassir <c>kkt/receipt</c> — прямая фискализация (в обход Init.Receipt Т‑Банка).
    /// Включается <c>cloudKassir:directFiscalization:enabled</c> + креды. Default off.
    /// Сплит денег — не здесь (Т‑Банк Deal/A2C).
    /// Docs: docs/Тестирование онлайн-чека – CloudKassir for Developers.html
    /// </summary>
    public class CloudKassirReceiptProvider : IFiscalReceiptProvider, IDisposable
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.Payments.CloudKassir.CloudKassirReceiptProvider.";
        #endregion

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly FiscalSettings _settings;
        private readonly HttpClient _http;
        private readonly bool _ownsHttp;

        public CloudKassirReceiptProvider(ICorrelationIdProvider correlationIdProvider)
            : this(correlationIdProvider, FiscalSettings.Load(), httpClient: null)
        {
        }

        internal CloudKassirReceiptProvider(
            ICorrelationIdProvider correlationIdProvider,
            FiscalSettings settings,
            HttpClient? httpClient)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (httpClient != null)
            {
                _http = httpClient;
                _ownsHttp = false;
            }
            else
            {
                _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
                _ownsHttp = true;
            }
        }

        public string ProviderName => "cloudKassir";

        public bool IsEnabled => _settings.IsDirectFiscalizationEnabled();

        public Task<FiscalReceiptResult> IssueIncomeForOrderAsync(FiscalIncomeReceiptRequest request)
            => SendReceiptAsync(request, isRefund: false);

        public Task<FiscalReceiptResult> IssueIncomeReturnForRefundAsync(FiscalRefundReceiptRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var income = new FiscalIncomeReceiptRequest
            {
                OrderId = request.OrderId,
                BuyerAccountId = request.BuyerAccountId,
                AmountTotal = request.Amount,
                AmountSeller = request.Amount,
                AmountCommission = 0,
                Currency = request.Currency,
                Description = request.Description ?? $"Возврат заказа {request.OrderId:D}",
                BuyerEmail = request.BuyerEmail,
                BuyerPhone = request.BuyerPhone,
                SupplierInn = request.SupplierInn,
                SupplierName = request.SupplierName,
                SupplierPhone = request.SupplierPhone,
                InvoiceId = request.InvoiceId ?? $"refund:{request.RefundId:N}"
            };
            return SendReceiptAsync(income, isRefund: true);
        }

        public async Task<FiscalReceiptStatusResult> GetReceiptStatusAsync(string providerReceiptId)
        {
            if (string.IsNullOrWhiteSpace(providerReceiptId))
                return FiscalReceiptStatusResult.Fail("providerReceiptId empty");

            if (!IsEnabled)
                return FiscalReceiptStatusResult.Fail("cloudKassir directFiscalization disabled");

            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(GetReceiptStatusAsync)}";

            try
            {
                var ck = _settings.DirectFiscalization;
                var body = new JObject { ["Id"] = providerReceiptId.Trim() };
                using var httpRequest = new HttpRequestMessage(
                    HttpMethod.Post,
                    CombineUrl(ck.ApiBaseUrl, "kkt/receipt/status/get"));
                ApplyBasicAuth(httpRequest, ck);
                httpRequest.Content = new StringContent(
                    body.ToString(Formatting.None), Encoding.UTF8, "application/json");

                using var response = await _http.SendAsync(httpRequest);
                var responseBody = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    logger.Warn(correlationId, null, methodName,
                        $"CloudKassir status HTTP {(int)response.StatusCode}: {Truncate(responseBody, 400)}", null);
                    return FiscalReceiptStatusResult.Fail($"HTTP {(int)response.StatusCode}");
                }

                var parsed = JObject.Parse(string.IsNullOrWhiteSpace(responseBody) ? "{}" : responseBody);
                var success = parsed.Value<bool?>("Success") ?? false;
                var model = parsed["Model"]?.Type == JTokenType.String
                    ? parsed.Value<string>("Model")
                    : parsed["Model"]?.ToString();
                if (!success)
                    return FiscalReceiptStatusResult.Fail(parsed.Value<string>("Message") ?? "Success=false");

                return FiscalReceiptStatusResult.Ok(model);
            }
            catch (Exception ex)
            {
                logger.Error(correlationId, null, methodName,
                    $"CloudKassir status failed id={providerReceiptId}: {ex.Message}", null, ex);
                return FiscalReceiptStatusResult.Fail(ex.Message);
            }
        }

        public bool VerifyWebhookHmac(string rawBody, string? contentHmac, string? xContentHmac)
        {
            if (!IsEnabled || string.IsNullOrEmpty(rawBody))
                return false;

            var secret = _settings.DirectFiscalization.ApiSecret;
            if (string.IsNullOrWhiteSpace(secret))
                return false;

            var expected = ComputeHmacSha256Base64(rawBody, secret);
            if (!string.IsNullOrWhiteSpace(xContentHmac)
                && FixedTimeEqualsBase64(expected, xContentHmac.Trim()))
                return true;

            if (!string.IsNullOrWhiteSpace(contentHmac)
                && FixedTimeEqualsBase64(expected, contentHmac.Trim()))
                return true;

            return false;
        }

        private async Task<FiscalReceiptResult> SendReceiptAsync(
            FiscalIncomeReceiptRequest request,
            bool isRefund)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(SendReceiptAsync)}";

            if (!IsEnabled)
                return FiscalReceiptResult.Skip("cloudKassir directFiscalization disabled or not configured");

            try
            {
                var ck = _settings.DirectFiscalization;
                var platformInn = DigitsOnly(ck.Inn);
                if (string.IsNullOrWhiteSpace(platformInn))
                    return FiscalReceiptResult.Fail("cloudKassir:directFiscalization:inn is required (KKT owner)");

                var invoiceId = !string.IsNullOrWhiteSpace(request.InvoiceId)
                    ? request.InvoiceId.Trim()
                    : $"order:{request.OrderId:N}";
                var amount = Math.Round(request.AmountTotal, 2, MidpointRounding.AwayFromZero);

                object? vat = string.Equals(ck.Vat, "null", StringComparison.OrdinalIgnoreCase)
                    ? null
                    : (int.TryParse(ck.Vat, out var vatInt) ? vatInt : ck.Vat);

                var customerReceipt = new JObject
                {
                    ["Items"] = new JArray
                    {
                        new JObject
                        {
                            ["Label"] = Truncate(request.Description ?? $"Билет заказ {request.OrderId:D}", 128),
                            ["Price"] = amount,
                            ["Quantity"] = 1.0,
                            ["Amount"] = amount,
                            ["Vat"] = vat == null ? JValue.CreateNull() : JToken.FromObject(vat),
                            ["Method"] = 4, // full payment
                            ["Object"] = 4, // service
                            ["AgentSign"] = ck.AgentSign,
                            ["PurveyorData"] = BuildPurveyor(request)
                        }
                    },
                    ["TaxationSystem"] = ck.TaxationSystem,
                    ["Amounts"] = new JObject { ["Electronic"] = amount }
                };

                if (!string.IsNullOrWhiteSpace(request.BuyerEmail))
                    customerReceipt["Email"] = request.BuyerEmail.Trim();
                if (!string.IsNullOrWhiteSpace(request.BuyerPhone))
                    customerReceipt["Phone"] = DigitsPhone(request.BuyerPhone);

                var body = new JObject
                {
                    // ИНН владельца ККТ (площадка), продавец — в PurveyorData.
                    ["Inn"] = platformInn,
                    ["Type"] = isRefund ? "IncomeReturn" : "Income",
                    ["InvoiceId"] = invoiceId,
                    ["AccountId"] = request.BuyerAccountId == Guid.Empty
                        ? null
                        : request.BuyerAccountId.ToString("D"),
                    ["CustomerReceipt"] = customerReceipt
                };

                using var httpRequest = new HttpRequestMessage(
                    HttpMethod.Post,
                    CombineUrl(ck.ApiBaseUrl, "kkt/receipt"));
                ApplyBasicAuth(httpRequest, ck);
                // Идемпотентность CloudKassir: повтор с тем же X-Request-ID → тот же результат (~1ч).
                httpRequest.Headers.TryAddWithoutValidation("X-Request-ID", invoiceId);
                httpRequest.Content = new StringContent(
                    body.ToString(Formatting.None), Encoding.UTF8, "application/json");

                using var response = await _http.SendAsync(httpRequest);
                var responseBody = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    logger.Warn(correlationId, null, methodName,
                        $"CloudKassir HTTP {(int)response.StatusCode}: {Truncate(responseBody, 400)}", null);
                    return FiscalReceiptResult.Fail($"HTTP {(int)response.StatusCode}");
                }

                var parsed = JObject.Parse(string.IsNullOrWhiteSpace(responseBody) ? "{}" : responseBody);
                var success = parsed.Value<bool?>("Success") ?? false;
                if (!success)
                {
                    var msg = parsed.Value<string>("Message") ?? parsed.Value<string>("Error") ?? "Success=false";
                    return FiscalReceiptResult.Fail(msg);
                }

                var model = parsed["Model"] as JObject;
                var receiptId = model?.Value<string>("Id")
                    ?? model?.Value<string>("ReceiptLocalUrl")
                    ?? invoiceId;

                logger.Debug(correlationId, null, methodName,
                    $"CloudKassir receipt OK order={request.OrderId:D} id={receiptId} invoice={invoiceId}", null);
                return FiscalReceiptResult.Ok(receiptId, invoiceId);
            }
            catch (Exception ex)
            {
                logger.Error(correlationId, null, methodName,
                    $"CloudKassir receipt failed order={request.OrderId:D}: {ex.Message}", null, ex);
                return FiscalReceiptResult.Fail(ex.Message);
            }
        }

        private static void ApplyBasicAuth(HttpRequestMessage httpRequest, CloudKassirDirectFiscalizationSettings ck)
        {
            var basic = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{ck.PublicId}:{ck.ApiSecret}"));
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        }

        private static JObject BuildPurveyor(FiscalIncomeReceiptRequest request)
        {
            var data = new JObject();
            var inn = DigitsOnly(request.SupplierInn);
            if (!string.IsNullOrWhiteSpace(inn))
                data["Inn"] = inn;
            if (!string.IsNullOrWhiteSpace(request.SupplierName))
                data["Name"] = Truncate(request.SupplierName, 64);
            var phone = DigitsPhone(request.SupplierPhone);
            if (!string.IsNullOrWhiteSpace(phone))
                data["Phone"] = phone;
            return data;
        }

        internal static string ComputeHmacSha256Base64(string message, string apiSecret)
        {
            var key = Encoding.UTF8.GetBytes(apiSecret);
            var data = Encoding.UTF8.GetBytes(message);
            using var hmac = new HMACSHA256(key);
            return Convert.ToBase64String(hmac.ComputeHash(data));
        }

        private static bool FixedTimeEqualsBase64(string expected, string actual)
        {
            try
            {
                var a = Convert.FromBase64String(expected);
                var b = Convert.FromBase64String(actual);
                return CryptographicOperations.FixedTimeEquals(a, b);
            }
            catch
            {
                return string.Equals(expected, actual, StringComparison.Ordinal);
            }
        }

        private static string CombineUrl(string baseUrl, string relative)
        {
            return baseUrl.TrimEnd('/') + "/" + relative.TrimStart('/');
        }

        private static string? DigitsOnly(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            var digits = new string(value.Where(char.IsDigit).ToArray());
            return string.IsNullOrEmpty(digits) ? null : digits;
        }

        private static string? DigitsPhone(string? value)
        {
            var digits = DigitsOnly(value);
            if (string.IsNullOrEmpty(digits))
                return null;
            if (digits.Length == 11 && digits.StartsWith('8'))
                digits = "7" + digits[1..];
            return digits;
        }

        private static string Truncate(string? value, int max)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Length <= max ? value : value[..max];
        }

        public void Dispose()
        {
            if (_ownsHttp)
                _http.Dispose();
        }
    }

    /// <summary>Default: фискализация выключена.</summary>
    public class NoOpFiscalReceiptProvider : IFiscalReceiptProvider
    {
        public string ProviderName => "none";
        public bool IsEnabled => false;

        public Task<FiscalReceiptResult> IssueIncomeForOrderAsync(FiscalIncomeReceiptRequest request)
            => Task.FromResult(FiscalReceiptResult.Skip("fiscal provider=none"));

        public Task<FiscalReceiptResult> IssueIncomeReturnForRefundAsync(FiscalRefundReceiptRequest request)
            => Task.FromResult(FiscalReceiptResult.Skip("fiscal provider=none"));

        public Task<FiscalReceiptStatusResult> GetReceiptStatusAsync(string providerReceiptId)
            => Task.FromResult(FiscalReceiptStatusResult.Fail("fiscal provider=none"));

        public bool VerifyWebhookHmac(string rawBody, string? contentHmac, string? xContentHmac)
            => false;
    }
}
