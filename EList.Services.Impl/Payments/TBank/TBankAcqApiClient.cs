using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace EList.Services.Impl.Payments.TBank
{
    /// <summary>
    /// Клиент acqapi Т‑Банка для регистрации точек Мультирасчётов (PDF «Регистрация…» v1.11):
    /// OAuth password grant → <c>/sm-register/register</c> (+ get/update).
    /// </summary>
    public class TBankAcqApiClient : IDisposable
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.Payments.TBank.TBankAcqApiClient.";
        #endregion

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly TBankPaymentSettings _settings;
        private readonly HttpClient _http;
        private readonly bool _ownsHttp;

        public TBankAcqApiClient(ICorrelationIdProvider correlationIdProvider, TBankPaymentSettings? settings = null)
            : this(correlationIdProvider, settings, httpClient: null)
        {
        }

        internal TBankAcqApiClient(
            ICorrelationIdProvider correlationIdProvider,
            TBankPaymentSettings? settings,
            HttpClient? httpClient)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _settings = settings ?? TBankPaymentSettings.Load();
            if (httpClient != null)
            {
                _http = httpClient;
                _ownsHttp = false;
            }
            else
            {
                _http = CreateHttpClient(_settings.AcqApi);
                _ownsHttp = true;
            }
        }

        public async Task<string> AuthorizeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureConfigured();

            var correlationId = _correlationIdProvider.Get();
            var methodName = $"{LOGGER_NAME}{nameof(AuthorizeAsync)}";
            var acq = _settings.AcqApi;

            using var request = new HttpRequestMessage(HttpMethod.Post, BuildUrl("oauth/token"));
            var basic = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{acq.ClientId}:{acq.ClientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = acq.Username,
                ["password"] = acq.Password
            });

            using var response = await _http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"acqapi OAuth failed: {(int)response.StatusCode} {response.StatusCode}; body={TruncateForLog(body)}");
            }

            var parsed = JsonConvert.DeserializeObject<TBankAcqApiOAuthResponse>(body);
            var token = parsed?.AccessToken;
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException(
                    $"acqapi OAuth не вернул access_token: {TruncateForLog(body)}");
            }

            logger.Debug(correlationId, null, methodName, "acqapi OAuth OK", null);
            return token.Trim();
        }

        public async Task<TBankAcqApiShopResponse> RegisterShopAsync(
            string accessToken,
            SellerOnboardingRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("accessToken is required", nameof(accessToken));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            EnsureConfigured();

            var payload = BuildRegisterPayload(request);
            var body = await SendJsonAsync(
                HttpMethod.Post,
                "sm-register/register",
                accessToken,
                payload,
                cancellationToken);

            var response = JsonConvert.DeserializeObject<TBankAcqApiShopResponse>(body)
                ?? throw new InvalidOperationException("acqapi register: пустой ответ");

            if (response.Errors is { Count: > 0 })
            {
                var detail = string.Join("; ", response.Errors.Select(e =>
                    $"{e.Field}: {e.DefaultMessage} ({e.RejectedValue})"));
                throw new InvalidOperationException($"acqapi register validation: {detail}");
            }

            return response;
        }

        public async Task<TBankAcqApiShopInfoResponse?> GetShopAsync(
            string accessToken,
            string shopCode,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("accessToken is required", nameof(accessToken));
            if (string.IsNullOrWhiteSpace(shopCode))
                throw new ArgumentException("shopCode is required", nameof(shopCode));

            EnsureConfigured();

            var body = await SendJsonAsync(
                HttpMethod.Get,
                $"sm-register/register/shop/{Uri.EscapeDataString(shopCode.Trim())}",
                accessToken,
                content: null,
                cancellationToken);

            return JsonConvert.DeserializeObject<TBankAcqApiShopInfoResponse>(body);
        }

        public async Task<string> UpdateShopAsync(
            string accessToken,
            string shopCode,
            object patchPayload,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("accessToken is required", nameof(accessToken));
            if (string.IsNullOrWhiteSpace(shopCode))
                throw new ArgumentException("shopCode is required", nameof(shopCode));
            if (patchPayload == null)
                throw new ArgumentNullException(nameof(patchPayload));

            EnsureConfigured();

            return await SendJsonAsync(
                HttpMethod.Patch,
                $"sm-register/register/{Uri.EscapeDataString(shopCode.Trim())}",
                accessToken,
                patchPayload,
                cancellationToken);
        }

        public void Dispose()
        {
            if (_ownsHttp)
                _http.Dispose();
        }

        private Dictionary<string, object?> BuildRegisterPayload(SellerOnboardingRequest request)
        {
            var billingDescriptor = Truncate(SanitizeBillingDescriptor(request.LegalName ?? request.OrganizationId.ToString("N")), 14);
            var legalName = string.IsNullOrWhiteSpace(request.LegalName) ? billingDescriptor : request.LegalName.Trim();
            var nameParts = SplitName(request.HeadName);
            var email = string.IsNullOrWhiteSpace(request.Email)
                ? "onboarding@tvoy-spot.ru"
                : request.Email.Trim();
            var phone = DigitsOnly(request.Phone) ?? "79000000000";
            var (zip, city, street) = ParseAddress(request.LegalAddress);
            var shopArticleId = Truncate(request.OrganizationId.ToString("N"), 32);
            var bankName = string.IsNullOrWhiteSpace(request.BankName) ? "Банк" : request.BankName.Trim();

            var payload = new Dictionary<string, object?>
            {
                ["shopArticleId"] = shopArticleId,
                ["billingDescriptor"] = billingDescriptor,
                ["fullName"] = legalName,
                ["name"] = Truncate(legalName, 512),
                ["inn"] = DigitsOnly(request.Inn),
                ["email"] = email,
                ["siteUrl"] = request.ReturnUrl ?? "https://tvoy-spot.ru",
                ["addresses"] = new[]
                {
                    new Dictionary<string, object?>
                    {
                        ["type"] = "legal",
                        ["zip"] = zip,
                        ["country"] = "RUS",
                        ["city"] = city,
                        ["street"] = street
                    }
                },
                ["ceo"] = new Dictionary<string, object?>
                {
                    ["firstName"] = nameParts.First,
                    ["lastName"] = nameParts.Last,
                    ["middleName"] = string.IsNullOrWhiteSpace(nameParts.Middle) ? null : nameParts.Middle,
                    ["phone"] = phone,
                    ["country"] = "RUS"
                },
                ["bankAccount"] = new Dictionary<string, object?>
                {
                    ["account"] = DigitsOnly(request.BankAccount),
                    ["bankName"] = bankName,
                    ["bik"] = DigitsOnly(request.Bik),
                    ["details"] = $"Возмещение по Мультирасчётам EList org {request.OrganizationId:N}"
                }
            };

            var ogrnDigits = DigitsOnly(request.Ogrn);
            if (!string.IsNullOrWhiteSpace(ogrnDigits) && long.TryParse(ogrnDigits, out var ogrnNum))
                payload["ogrn"] = ogrnNum;
            else if (!string.IsNullOrWhiteSpace(ogrnDigits))
                payload["ogrn"] = ogrnDigits;

            var kpp = DigitsOnly(request.Kpp);
            if (!string.IsNullOrWhiteSpace(kpp))
                payload["kpp"] = kpp;

            return payload;
        }

        private async Task<string> SendJsonAsync(
            HttpMethod method,
            string relativeUrl,
            string accessToken,
            object? content,
            CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(method, BuildUrl(relativeUrl));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Trim());
            if (content != null)
            {
                var json = JsonConvert.SerializeObject(content, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            using var response = await _http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"acqapi {method} {relativeUrl} failed: {(int)response.StatusCode} {response.StatusCode}; body={TruncateForLog(body)}");
            }

            return body ?? string.Empty;
        }

        private Uri BuildUrl(string relative)
        {
            var baseUrl = _settings.AcqApi.BaseUrl.TrimEnd('/') + "/";
            return new Uri(new Uri(baseUrl), relative.TrimStart('/'));
        }

        private void EnsureConfigured()
        {
            if (!_settings.AcqApi.IsConfigured())
            {
                throw new InvalidOperationException(
                    "acqapi не настроен: задайте payments__tbank__acqApi__username и payments__tbank__acqApi__password "
                    + "(OAuth password grant; Basic client по умолчанию partner:partner).");
            }
        }

        private static HttpClient CreateHttpClient(TBankAcqApiSettings acq)
        {
            HttpMessageHandler handler;
            if (!string.IsNullOrWhiteSpace(acq.ClientCertificatePath))
            {
                var certificate = string.IsNullOrWhiteSpace(acq.ClientCertificatePassword)
                    ? new X509Certificate2(acq.ClientCertificatePath)
                    : new X509Certificate2(acq.ClientCertificatePath, acq.ClientCertificatePassword);

                var sockets = new SocketsHttpHandler
                {
                    SslOptions =
                    {
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                        ClientCertificates = new X509CertificateCollection { certificate },
                        RemoteCertificateValidationCallback = static (_, _, _, _) => true
                    }
                };
                handler = sockets;
            }
            else
            {
                handler = new SocketsHttpHandler
                {
                    SslOptions =
                    {
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                        RemoteCertificateValidationCallback = static (_, _, _, _) => true
                    }
                };
            }

            return new HttpClient(handler, disposeHandler: true)
            {
                Timeout = DefaultTimeout
            };
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

        private static (string Zip, string City, string Street) ParseAddress(string? legalAddress)
        {
            var raw = (legalAddress ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
                return ("000000", "Moscow", "не указан");

            var zipMatch = System.Text.RegularExpressions.Regex.Match(raw, @"\b(\d{6})\b");
            var zip = zipMatch.Success ? zipMatch.Groups[1].Value : "000000";
            return (zip, "Moscow", Truncate(raw, 255));
        }

        private static string TruncateForLog(string? body)
        {
            if (string.IsNullOrEmpty(body))
                return string.Empty;
            return body.Length <= 800 ? body : body[..800] + "…";
        }
    }

    public class TBankAcqApiOAuthResponse
    {
        [JsonProperty("access_token")]
        public string? AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string? TokenType { get; set; }

        [JsonProperty("expires_in")]
        public long? ExpiresIn { get; set; }
    }

    public class TBankAcqApiShopResponse
    {
        [JsonProperty("code")]
        public string? Code { get; set; }

        [JsonProperty("shopCode")]
        public JToken? ShopCode { get; set; }

        [JsonProperty("terminals")]
        public JArray? Terminals { get; set; }

        [JsonProperty("errors")]
        public List<TBankAcqApiValidationError>? Errors { get; set; }

        [JsonProperty("status")]
        public string? Status { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }

        public string? ResolveShopCode()
        {
            if (ShopCode == null || ShopCode.Type == JTokenType.Null)
                return null;
            var value = ShopCode.Type == JTokenType.String
                ? ShopCode.Value<string>()
                : ShopCode.ToString();
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }

    public class TBankAcqApiValidationError
    {
        [JsonProperty("field")]
        public string? Field { get; set; }

        [JsonProperty("defaultMessage")]
        public string? DefaultMessage { get; set; }

        [JsonProperty("rejectedValue")]
        public string? RejectedValue { get; set; }

        [JsonProperty("code")]
        public string? Code { get; set; }
    }

    public class TBankAcqApiShopInfoResponse
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("inn")]
        public string? Inn { get; set; }

        [JsonProperty("email")]
        public string? Email { get; set; }

        [JsonProperty("shopCode")]
        public JToken? ShopCode { get; set; }
    }
}
