using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using EList.Cache.Base;
using EList.Cache.Memory;
using EList.Common.Configuration;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Location;
using EList.Services.Interfaces;
using Newtonsoft.Json.Linq;
using NLog;

namespace EList.Services.Impl
{
    /// <summary>
    /// Обёртка над Geocoder API Яндекса. Кэширует успешные ответы в MemoryCache (≤ 30 суток по ToS).
    /// </summary>
    public class GeocodeService : IGeocodeService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.GeocodeService.";
        #endregion

        private const string DefaultGeocodeBaseUrl = "https://geocode-maps.yandex.ru/1.x/";
        private const string DefaultLang = "ru_RU";
        private const int ReverseCoordDecimals = 4;

        private static readonly HttpClient Http = CreateHttpClient();
        private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IMemoryCache _memoryCache;
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly TimeSpan _timeout;
        private readonly TimeSpan _cacheTtl;

        public GeocodeService(ICorrelationIdProvider correlationIdProvider, IMemoryCache memoryCache)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

            _baseUrl = ConfigurationManager.AppSettings.Contains("yandexService:geocode:baseUrl")
                ? ConfigurationManager.AppSettings["yandexService:geocode:baseUrl"]
                : DefaultGeocodeBaseUrl;

            _apiKey = ConfigurationManager.AppSettings.Contains("yandexService:geocode:apiKey")
                ? ConfigurationManager.AppSettings["yandexService:geocode:apiKey"]
                : string.Empty;

            _timeout = ConfigurationManager.AppSettings.Contains("yandexService:geocode:timeout")
                ? TimeSpan.Parse(ConfigurationManager.AppSettings["yandexService:geocode:timeout"])
                : TimeSpan.FromSeconds(10);

            if (ConfigurationManager.AppSettings.Contains("yandexService:geocode:cacheDays")
                && int.TryParse(ConfigurationManager.AppSettings["yandexService:geocode:cacheDays"], out var days)
                && days > 0)
            {
                // Условия Яндекса: временный кэш не дольше 30 суток
                _cacheTtl = TimeSpan.FromDays(Math.Min(days, 30));
            }
            else
            {
                _cacheTtl = DefaultCacheTimeouts.GeocodeTimeout;
            }
        }

        public async Task<CommandResult<GeocodedPlace?>> GeocodeAsync(string query, string? lang = null)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GeocodeAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            var normalized = NormalizeQuery(query);
            if (string.IsNullOrWhiteSpace(normalized))
                return CommandResult<GeocodedPlace?>.Fail(ErrorCode.IsNullOrEmpty, "Пустой запрос геокодирования");

            if (string.IsNullOrWhiteSpace(_apiKey))
                return CommandResult<GeocodedPlace?>.Fail(ErrorCode.InternalError, "Геокодер не настроен (yandexService:geocode:apiKey)");

            var effectiveLang = string.IsNullOrWhiteSpace(lang) ? DefaultLang : lang.Trim();
            var cacheKey = KeyBuilder.Build("geocode", $"fwd:{effectiveLang}:{normalized}");

            var place = await GetOrFetchAsync(
                cacheKey,
                () => FetchFromYandexAsync(correlationId, methodName, normalized, effectiveLang, kind: null));

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<GeocodedPlace?>(place);
        }

        public async Task<CommandResult<GeocodedPlace?>> ReverseGeocodeAsync(
            double lat, double lng, string? kind = null, string? lang = null)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(ReverseGeocodeAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (lat is < -90 or > 90 || lng is < -180 or > 180)
                return CommandResult<GeocodedPlace?>.Fail(ErrorCode.InvalidValue, "Некорректные координаты");

            if (string.IsNullOrWhiteSpace(_apiKey))
                return CommandResult<GeocodedPlace?>.Fail(ErrorCode.InternalError, "Геокодер не настроен (yandexService:geocode:apiKey)");

            var effectiveLang = string.IsNullOrWhiteSpace(lang) ? DefaultLang : lang.Trim();
            var roundedLat = Math.Round(lat, ReverseCoordDecimals, MidpointRounding.AwayFromZero);
            var roundedLng = Math.Round(lng, ReverseCoordDecimals, MidpointRounding.AwayFromZero);
            var latStr = roundedLat.ToString(CultureInfo.InvariantCulture);
            var lngStr = roundedLng.ToString(CultureInfo.InvariantCulture);
            var kindPart = string.IsNullOrWhiteSpace(kind) ? string.Empty : $":{kind.Trim()}";
            var cacheKey = KeyBuilder.Build("geocode", $"rev:{effectiveLang}:{latStr},{lngStr}{kindPart}");
            var geocodeParam = $"{lngStr},{latStr}";

            var place = await GetOrFetchAsync(
                cacheKey,
                () => FetchFromYandexAsync(correlationId, methodName, geocodeParam, effectiveLang, kind));

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<GeocodedPlace?>(place);
        }

        /// <summary>
        /// GetOrUpdate с single-flight; промахи Яндекса не кэшируем (cacheDataCriteriaFunc).
        /// </summary>
        private async Task<GeocodedPlace?> GetOrFetchAsync(string cacheKey, Func<Task<GeocodedPlace?>> fetch)
        {
            return await _memoryCache.GetOrUpdateAsync(
                cacheKey,
                async () =>
                {
                    var place = await fetch();
                    return new CacheContent<GeocodedPlace>(_cacheTtl, place!);
                },
                cacheDataCriteriaFunc: p => Task.FromResult(p != null));
        }

        private async Task<GeocodedPlace?> FetchFromYandexAsync(
            string correlationId,
            string methodName,
            string geocodeParam,
            string lang,
            string? kind)
        {
            var url = BuildGeocodeUrl(geocodeParam, lang, kind);

            try
            {
                using var cts = new CancellationTokenSource(_timeout);
                // Не логируем URL с apikey и текстовый адрес из ответа
                logger.Debug(
                    correlationId,
                    null,
                    methodName,
                    "Yandex geocode request",
                    null,
                    null,
                    new Dictionary<string, object>
                    {
                        ["lang"] = lang,
                        ["kind"] = kind ?? string.Empty,
                        ["mode"] = IsCoordinatePair(geocodeParam) ? "reverse" : "forward"
                    });

                using var response = await Http.GetAsync(url, cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    logger.Warn(correlationId, null, methodName,
                        $"Yandex geocode HTTP {(int)response.StatusCode}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync(cts.Token);
                return ParseGeoObject(json);
            }
            catch (OperationCanceledException)
            {
                logger.Warn(correlationId, null, methodName, "Yandex geocode timeout");
                return null;
            }
            catch (Exception ex)
            {
                logger.Error(correlationId, null, methodName, "Yandex geocode failed", null, ex);
                return null;
            }
        }

        private string BuildGeocodeUrl(string geocodeParam, string lang, string? kind)
        {
            var sb = new StringBuilder();
            sb.Append(_baseUrl.TrimEnd('/'));
            sb.Append("/?apikey=");
            sb.Append(Uri.EscapeDataString(_apiKey));
            sb.Append("&geocode=");
            sb.Append(Uri.EscapeDataString(geocodeParam));
            sb.Append("&format=json&results=1&lang=");
            sb.Append(Uri.EscapeDataString(lang));
            if (!string.IsNullOrWhiteSpace(kind))
            {
                sb.Append("&kind=");
                sb.Append(Uri.EscapeDataString(kind.Trim()));
            }

            return sb.ToString();
        }

        private static bool IsCoordinatePair(string geocodeParam)
        {
            var parts = geocodeParam.Split(',');
            return parts.Length == 2
                   && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out _)
                   && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }

        private static GeocodedPlace? ParseGeoObject(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            var root = JObject.Parse(json);
            var member = root["response"]?["GeoObjectCollection"]?["featureMember"]?.FirstOrDefault()?["GeoObject"];
            if (member == null)
                return null;

            var pos = member["Point"]?["pos"]?.ToString()?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (pos == null || pos.Length < 2)
                return null;

            if (!double.TryParse(pos[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lng)
                || !double.TryParse(pos[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
                return null;

            var address = member["metaDataProperty"]?["GeocoderMetaData"]?["text"]?.ToString()
                          ?? string.Empty;

            return new GeocodedPlace
            {
                Lat = lat,
                Lng = lng,
                Address = address
            };
        }

        private static string NormalizeQuery(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return string.Empty;

            return Whitespace.Replace(query.Trim().ToLowerInvariant(), " ");
        }

        private static HttpClient CreateHttpClient()
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                AllowAutoRedirect = false
            };
            return new HttpClient(handler, disposeHandler: true)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }
    }
}
