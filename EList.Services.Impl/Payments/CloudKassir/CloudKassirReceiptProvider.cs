using System.Net.Http.Headers;
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
    /// Каркас CloudKassir <c>kkt/receipt</c> (54‑ФЗ). Включается через fiscal:enabled + cloudKassir creds.
    /// Сплит денег — не здесь (Т‑Банк Deal/A2C).
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

        public bool IsEnabled => _settings.IsCloudKassir();

        public Task<FiscalReceiptResult> IssueIncomeForOrderAsync(FiscalIncomeReceiptRequest request)
            => SendReceiptAsync(request, isRefund: false);

        public Task<FiscalReceiptResult> IssueIncomeReturnForRefundAsync(FiscalRefundReceiptRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Маппинг в тот же Income-формат с Type=IncomeReturn.
            var income = new FiscalIncomeReceiptRequest
            {
                OrderId = request.OrderId,
                AmountTotal = request.Amount,
                AmountSeller = request.Amount,
                AmountCommission = 0,
                Currency = request.Currency,
                Description = request.Description ?? $"Возврат заказа {request.OrderId:D}",
                BuyerEmail = request.BuyerEmail,
                BuyerPhone = request.BuyerPhone
            };
            return SendReceiptAsync(income, isRefund: true, invoiceIdOverride: $"refund:{request.RefundId:N}");
        }

        private async Task<FiscalReceiptResult> SendReceiptAsync(
            FiscalIncomeReceiptRequest request,
            bool isRefund,
            string? invoiceIdOverride = null)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(SendReceiptAsync)}";

            if (!IsEnabled)
                return FiscalReceiptResult.Skip("fiscal disabled or cloudKassir not configured");

            try
            {
                var ck = _settings.CloudKassir;
                var invoiceId = invoiceIdOverride ?? $"order:{request.OrderId:N}";
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
                    ["Inn"] = DigitsOnly(request.SupplierInn) ?? string.Empty,
                    ["Type"] = isRefund ? "IncomeReturn" : "Income",
                    ["InvoiceId"] = invoiceId,
                    ["AccountId"] = request.BuyerAccountId.ToString("D"),
                    ["CustomerReceipt"] = customerReceipt
                };

                using var httpRequest = new HttpRequestMessage(
                    HttpMethod.Post,
                    CombineUrl(ck.ApiBaseUrl, "kkt/receipt"));
                var basic = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{ck.PublicId}:{ck.ApiSecret}"));
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
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
                    $"CloudKassir receipt OK order={request.OrderId:D} id={receiptId}", null);
                return FiscalReceiptResult.Ok(receiptId);
            }
            catch (Exception ex)
            {
                logger.Error(correlationId, null, methodName,
                    $"CloudKassir receipt failed order={request.OrderId:D}: {ex.Message}", null, ex);
                return FiscalReceiptResult.Fail(ex.Message);
            }
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
    }
}
