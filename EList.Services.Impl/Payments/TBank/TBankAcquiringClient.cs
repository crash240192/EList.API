using System.Globalization;
using EList.Common.CorrelationId;
using EList.Common.HttpRestClient;
using EList.Common.Logger;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace EList.Services.Impl.Payments.TBank
{
    /// <summary>
    /// HTTP-клиент эквайринга Т-Банка: Init / GetState / Cancel.
    /// Транспорт — <see cref="HttpRestClient2"/> (общий пул, логирование, SSL как у DaData).
    /// </summary>
    public class TBankAcquiringClient
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.Payments.TBank.TBankAcquiringClient.";
        #endregion

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly TBankPaymentSettings _settings;

        public TBankAcquiringClient(ICorrelationIdProvider correlationIdProvider, TBankPaymentSettings? settings = null)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _settings = settings ?? TBankPaymentSettings.Load();
        }

        public Task<TBankApiResponse> InitAsync(TBankInitRequest request, CancellationToken cancellationToken = default)
            => PostSignedAsync("Init", request, cancellationToken);

        public Task<TBankApiResponse> GetStateAsync(string paymentId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(paymentId))
                throw new ArgumentException("paymentId is required", nameof(paymentId));

            var request = new TBankPaymentIdRequest
            {
                TerminalKey = _settings.TerminalKey,
                PaymentId = paymentId.Trim()
            };
            return PostSignedAsync("GetState", request, cancellationToken);
        }

        public Task<TBankApiResponse> CancelAsync(
            string paymentId,
            long? amountKopecks = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(paymentId))
                throw new ArgumentException("paymentId is required", nameof(paymentId));

            var request = new TBankPaymentIdRequest
            {
                TerminalKey = _settings.TerminalKey,
                PaymentId = paymentId.Trim(),
                Amount = amountKopecks
            };
            return PostSignedAsync("Cancel", request, cancellationToken);
        }

        public bool VerifyNotificationToken(JObject payload)
        {
            _settings.EnsureAcquiringConfigured();
            var token = payload["Token"]?.ToString();
            return TBankTokenSigner.VerifyJsonObject(payload, _settings.Password, token);
        }

        private async Task<TBankApiResponse> PostSignedAsync<TRequest>(
            string method,
            TRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _settings.EnsureAcquiringConfigured();

            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{method}";

            // Token считается по корневым скалярам (Shops/Receipt не входят) — подписываем уже сериализованный JSON.
            var jObject = JObject.Parse(JsonConvert.SerializeObject(request));
            jObject.Remove("Token");
            jObject["Token"] = TBankTokenSigner.SignJsonObject(jObject, _settings.Password);
            var body = jObject.ToString(Formatting.None);

            logger.Debug(correlationId, null, methodName, $"POST {_settings.ApiBaseUrl.TrimEnd('/')}/{method}", null);

            var http = new HttpRestClient2(correlationId, _settings.ApiBaseUrl.TrimEnd('/'));
            TBankApiResponse? parsed;
            try
            {
                parsed = await http.PostAsync<TBankApiResponse>(method, body, DefaultTimeout);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Т-Банк {method}: HTTP/транспортная ошибка: {ex.Message}", ex);
            }

            if (parsed == null)
                throw new InvalidOperationException($"Т-Банк {method}: пустой ответ");

            if (!parsed.Success)
                throw new InvalidOperationException($"Т-Банк {method}: {FormatError(parsed)}");

            return parsed;
        }

        private static string FormatError(TBankApiResponse response)
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

        public static long ToKopecks(decimal amount)
        {
            return (long)Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
        }

        public static decimal FromKopecks(long kopecks)
        {
            return kopecks / 100m;
        }

        public static string FormatKopecks(long kopecks)
            => kopecks.ToString(CultureInfo.InvariantCulture);
    }
}
