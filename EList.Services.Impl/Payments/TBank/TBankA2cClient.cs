using EList.Common.CorrelationId;
using EList.Common.HttpRestClient;
using EList.Common.Logger;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace EList.Services.Impl.Payments.TBank
{
    /// <summary>
    /// Клиент A2C_V2 Мультирасчётов: <c>/e2c/v2/Init</c>, <c>/Payment</c>, <c>/GetState</c>.
    /// Выплата партнёру — <c>PartnerId</c> = shopCode точки.
    /// </summary>
    public class TBankA2cClient
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.Payments.TBank.TBankA2cClient.";
        #endregion

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly TBankPaymentSettings _settings;

        public TBankA2cClient(ICorrelationIdProvider correlationIdProvider, TBankPaymentSettings? settings = null)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _settings = settings ?? TBankPaymentSettings.Load();
        }

        public Task<TBankA2cApiResponse> InitAsync(TBankA2cInitRequest request, CancellationToken cancellationToken = default)
            => PostSignedAsync("Init", request, cancellationToken);

        public Task<TBankA2cApiResponse> PaymentAsync(string paymentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(paymentId))
                throw new ArgumentException("paymentId is required", nameof(paymentId));

            var request = new TBankA2cPaymentRequest
            {
                TerminalKey = _settings.TerminalKey,
                PaymentId = paymentId.Trim()
            };
            return PostSignedAsync("Payment", request, cancellationToken);
        }

        public Task<TBankA2cApiResponse> GetStateAsync(string paymentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(paymentId))
                throw new ArgumentException("paymentId is required", nameof(paymentId));

            var request = new TBankA2cPaymentRequest
            {
                TerminalKey = _settings.TerminalKey,
                PaymentId = paymentId.Trim()
            };
            return PostSignedAsync("GetState", request, cancellationToken);
        }

        private async Task<TBankA2cApiResponse> PostSignedAsync<TRequest>(
            string method,
            TRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _settings.EnsureAcquiringConfigured();

            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{method}";
            var baseUrl = _settings.ResolveE2cApiBaseUrl().TrimEnd('/');

            var jObject = JObject.Parse(JsonConvert.SerializeObject(request));
            jObject.Remove("Token");
            jObject["Token"] = TBankTokenSigner.SignJsonObject(jObject, _settings.Password);
            var body = jObject.ToString(Formatting.None);

            logger.Debug(correlationId, null, methodName, $"POST {baseUrl}/{method}", null);

            var http = new HttpRestClient2(correlationId, baseUrl);
            TBankA2cApiResponse? parsed;
            try
            {
                parsed = await http.PostAsync<TBankA2cApiResponse>(method, body, DefaultTimeout);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Т‑Банк e2c {method}: HTTP/транспортная ошибка: {ex.Message}", ex);
            }

            if (parsed == null)
                throw new InvalidOperationException($"Т‑Банк e2c {method}: пустой ответ");

            if (!parsed.Success)
                throw new InvalidOperationException($"Т‑Банк e2c {method}: {FormatError(parsed)}");

            return parsed;
        }

        private static string FormatError(TBankA2cApiResponse response)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(response.ErrorCode))
                parts.Add($"ErrorCode={response.ErrorCode}");
            if (!string.IsNullOrWhiteSpace(response.Message))
                parts.Add(response.Message);
            if (!string.IsNullOrWhiteSpace(response.Details))
                parts.Add(response.Details);
            return parts.Count == 0 ? "неизвестная ошибка" : string.Join("; ", parts);
        }
    }

    public class TBankA2cInitRequest
    {
        [JsonProperty("TerminalKey")]
        public string TerminalKey { get; set; } = string.Empty;

        [JsonProperty("OrderId")]
        public string OrderId { get; set; } = string.Empty;

        /// <summary>Идентификатор получателя (≤16 символов): телефон/email/иной id.</summary>
        [JsonProperty("PaymentRecipientId")]
        public string PaymentRecipientId { get; set; } = string.Empty;

        /// <summary>DealId сделки (число или строка — банк принимает Number).</summary>
        [JsonProperty("DealId")]
        public object DealId { get; set; } = string.Empty;

        [JsonProperty("PartnerId")]
        public string PartnerId { get; set; } = string.Empty;

        [JsonProperty("Amount")]
        public long Amount { get; set; }

        [JsonProperty("FinalPayout", NullValueHandling = NullValueHandling.Ignore)]
        public bool? FinalPayout { get; set; }

        [JsonProperty("Token")]
        public string Token { get; set; } = string.Empty;
    }

    public class TBankA2cPaymentRequest
    {
        [JsonProperty("TerminalKey")]
        public string TerminalKey { get; set; } = string.Empty;

        [JsonProperty("PaymentId")]
        public string PaymentId { get; set; } = string.Empty;

        [JsonProperty("Token")]
        public string Token { get; set; } = string.Empty;
    }

    public class TBankA2cApiResponse
    {
        [JsonProperty("Success")]
        public bool Success { get; set; }

        [JsonProperty("ErrorCode")]
        public string? ErrorCode { get; set; }

        [JsonProperty("Message")]
        public string? Message { get; set; }

        [JsonProperty("Details")]
        public string? Details { get; set; }

        [JsonProperty("TerminalKey")]
        public string? TerminalKey { get; set; }

        [JsonProperty("Status")]
        public string? Status { get; set; }

        [JsonProperty("PaymentId")]
        public string? PaymentId { get; set; }

        [JsonProperty("OrderId")]
        public string? OrderId { get; set; }

        [JsonProperty("Amount")]
        public long? Amount { get; set; }
    }
}
