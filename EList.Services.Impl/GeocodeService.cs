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
    /// Обёртка над Nominatim (OSM). Кэширует успешные ответы в MemoryCache.
    /// Соблюдает ~1 req/s к публичному Nominatim (семафор + интервал).
    /// </summary>
    public class GeocodeService : IGeocodeService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.GeocodeService.";
        #endregion

        private const string DefaultNominatimBaseUrl = "https://nominatim.openstreetmap.org/";
        private const string DefaultUserAgent = "EList/3.0 (geocode; https://github.com/crash240192/elist.api)";
        private const string DefaultLang = "ru";
        private const int ReverseCoordDecimals = 4;
        private const int DefaultMinIntervalMs = 1100;

        private static readonly HttpClient Http = CreateHttpClient();
        private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);
        private static readonly SemaphoreSlim NominatimGate = new(1, 1);
        private static DateTimeOffset _lastNominatimUtc = DateTimeOffset.MinValue;

        private static readonly HashSet<string> LocalityPlaceTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "city", "town", "village", "municipality", "hamlet", "suburb", "neighbourhood",
            "city_district", "quarter", "borough", "administrative"
        };

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IMemoryCache _memoryCache;
        private readonly string _baseUrl;
        private readonly string _userAgent;
        private readonly TimeSpan _timeout;
        private readonly TimeSpan _cacheTtl;
        private readonly int _minIntervalMs;

        public GeocodeService(ICorrelationIdProvider correlationIdProvider, IMemoryCache memoryCache)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

            // Новый блок geocode:*; fallback на legacy yandexService:geocode:*
            _baseUrl = ReadSetting("geocode:baseUrl")
                       ?? ReadSetting("yandexService:geocode:baseUrl")
                       ?? DefaultNominatimBaseUrl;
            if (!_baseUrl.EndsWith('/'))
                _baseUrl += "/";

            _userAgent = ReadSetting("geocode:userAgent")
                         ?? DefaultUserAgent;

            var timeoutRaw = ReadSetting("geocode:timeout") ?? ReadSetting("yandexService:geocode:timeout");
            _timeout = timeoutRaw != null && TimeSpan.TryParse(timeoutRaw, out var t)
                ? t
                : TimeSpan.FromSeconds(10);

            var cacheDaysRaw = ReadSetting("geocode:cacheDays") ?? ReadSetting("yandexService:geocode:cacheDays");
            if (cacheDaysRaw != null && int.TryParse(cacheDaysRaw, out var days) && days > 0)
                _cacheTtl = TimeSpan.FromDays(Math.Min(days, 30));
            else
                _cacheTtl = DefaultCacheTimeouts.GeocodeTimeout;

            var intervalRaw = ReadSetting("geocode:minIntervalMs");
            _minIntervalMs = intervalRaw != null && int.TryParse(intervalRaw, out var ms) && ms >= 0
                ? ms
                : DefaultMinIntervalMs;
        }

        public async Task<CommandResult<GeocodedPlace?>> GeocodeAsync(
            string query, string? lang = null, bool cacheOnly = false)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GeocodeAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            var normalized = NormalizeQuery(query);
            if (string.IsNullOrWhiteSpace(normalized))
                return CommandResult<GeocodedPlace?>.Fail(ErrorCode.IsNullOrEmpty, "Пустой запрос геокодирования");

            var effectiveLang = NormalizeLang(lang);
            var cacheKey = BuildForwardCacheKey(effectiveLang, normalized);

            if (cacheOnly)
            {
                var cached = await _memoryCache.GetAsync<GeocodedPlace>(cacheKey);
                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return new CommandResult<GeocodedPlace?>(cached);
            }

            var place = await GetOrFetchAsync(
                cacheKey,
                () => FetchSearchFirstAsync(correlationId, methodName, normalized, effectiveLang));

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<GeocodedPlace?>(place);
        }

        public async Task<CommandResult<GeocodedPlace?>> ReverseGeocodeAsync(
            double lat, double lng, string? kind = null, string? lang = null, bool cacheOnly = false)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(ReverseGeocodeAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (lat is < -90 or > 90 || lng is < -180 or > 180)
                return CommandResult<GeocodedPlace?>.Fail(ErrorCode.InvalidValue, "Некорректные координаты");

            var effectiveLang = NormalizeLang(lang);
            var cacheKey = BuildReverseCacheKey(effectiveLang, lat, lng, kind);

            if (cacheOnly)
            {
                var cached = await _memoryCache.GetAsync<GeocodedPlace>(cacheKey);
                logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
                return new CommandResult<GeocodedPlace?>(cached);
            }

            var roundedLat = Math.Round(lat, ReverseCoordDecimals, MidpointRounding.AwayFromZero);
            var roundedLng = Math.Round(lng, ReverseCoordDecimals, MidpointRounding.AwayFromZero);

            var place = await GetOrFetchAsync(
                cacheKey,
                () => FetchReverseAsync(correlationId, methodName, roundedLat, roundedLng, kind, effectiveLang));

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<GeocodedPlace?>(place);
        }

        public async Task<CommandResult<List<GeocodeSuggestItem>>> SuggestAsync(
            string query, int limit = 8, string? lang = null)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SuggestAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            var normalized = NormalizeQuery(query);
            if (normalized.Length < 2)
                return new CommandResult<List<GeocodeSuggestItem>>(new List<GeocodeSuggestItem>());

            var effectiveLang = NormalizeLang(lang);
            var capped = Math.Clamp(limit, 1, 15);
            var cacheKey = KeyBuilder.Build("geocode", $"sug:{effectiveLang}:{capped}:{normalized}");

            var items = await _memoryCache.GetOrUpdateAsync(
                cacheKey,
                async () =>
                {
                    var list = await FetchSuggestAsync(correlationId, methodName, normalized, capped, effectiveLang);
                    return new CacheContent<List<GeocodeSuggestItem>>(_cacheTtl, list);
                },
                cacheDataCriteriaFunc: list => Task.FromResult(list != null && list.Count > 0));

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return new CommandResult<List<GeocodeSuggestItem>>(items ?? new List<GeocodeSuggestItem>());
        }

        public async Task<CommandResult> WarmCacheAsync(GeocodeCacheWarmRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(WarmCacheAsync)}";
            logger.Debug(correlationId, null, methodName, "Method started", null);

            if (request?.Place == null)
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Пустой Place для прогрева кэша");

            if (double.IsNaN(request.Place.Lat) || double.IsNaN(request.Place.Lng))
                return CommandResult.Fail(ErrorCode.InvalidValue, "Некорректные координаты Place");

            var effectiveLang = NormalizeLang(request.Lang);
            string cacheKey;

            var normalizedQuery = NormalizeQuery(request.Query);
            if (!string.IsNullOrWhiteSpace(normalizedQuery))
            {
                cacheKey = BuildForwardCacheKey(effectiveLang, normalizedQuery);
            }
            else if (request.Lat != null && request.Lng != null)
            {
                cacheKey = BuildReverseCacheKey(effectiveLang, request.Lat.Value, request.Lng.Value, request.Kind);
            }
            else
            {
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Укажите Query или Lat/Lng для ключа кэша");
            }

            var toStore = new GeocodedPlace
            {
                Lat = request.Place.Lat,
                Lng = request.Place.Lng,
                Address = request.Place.Address ?? string.Empty
            };

            await _memoryCache.GetOrUpdateAsync(
                cacheKey,
                () => Task.FromResult(new CacheContent<GeocodedPlace>(_cacheTtl, toStore)));

            logger.Debug(correlationId, null, methodName, "Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        private static string? ReadSetting(string key)
            => ConfigurationManager.AppSettings.Contains(key)
                ? ConfigurationManager.AppSettings[key]
                : null;

        private static string NormalizeLang(string? lang)
        {
            if (string.IsNullOrWhiteSpace(lang))
                return DefaultLang;
            // Nominatim: ru; legacy Яндекс: ru_RU → ru
            var t = lang.Trim();
            var under = t.IndexOf('_');
            if (under > 0)
                t = t[..under];
            var dash = t.IndexOf('-');
            if (dash > 0)
                t = t[..dash];
            return t.ToLowerInvariant();
        }

        private static string BuildForwardCacheKey(string lang, string normalizedQuery)
            => KeyBuilder.Build("geocode", $"fwd:{lang}:{normalizedQuery}");

        private static string BuildReverseCacheKey(string lang, double lat, double lng, string? kind)
        {
            var roundedLat = Math.Round(lat, ReverseCoordDecimals, MidpointRounding.AwayFromZero);
            var roundedLng = Math.Round(lng, ReverseCoordDecimals, MidpointRounding.AwayFromZero);
            var latStr = roundedLat.ToString(CultureInfo.InvariantCulture);
            var lngStr = roundedLng.ToString(CultureInfo.InvariantCulture);
            var kindPart = string.IsNullOrWhiteSpace(kind) ? string.Empty : $":{kind.Trim()}";
            return KeyBuilder.Build("geocode", $"rev:{lang}:{latStr},{lngStr}{kindPart}");
        }

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

        private async Task<GeocodedPlace?> FetchSearchFirstAsync(
            string correlationId, string methodName, string query, string lang)
        {
            var url = BuildSearchUrl(query, lang, limit: 1, addressDetails: false);
            var json = await GetNominatimJsonAsync(correlationId, methodName, url, "search");
            if (json == null)
                return null;

            var arr = json as JArray;
            var first = arr?.FirstOrDefault() as JObject;
            return first == null ? null : ParseSearchHit(first);
        }

        private async Task<GeocodedPlace?> FetchReverseAsync(
            string correlationId, string methodName, double lat, double lng, string? kind, string lang)
        {
            var url = BuildReverseUrl(lat, lng, lang);
            var json = await GetNominatimJsonAsync(correlationId, methodName, url, "reverse");
            if (json is not JObject obj)
                return null;

            var display = obj["display_name"]?.ToString();
            if (string.IsNullOrWhiteSpace(display))
                return null;

            // kind=locality → предпочитаем название города/нас. пункта
            if (string.Equals(kind, "locality", StringComparison.OrdinalIgnoreCase))
            {
                var shortName = ExtractLocalityName(obj["address"] as JObject);
                if (!string.IsNullOrWhiteSpace(shortName))
                    display = shortName;
            }

            if (!TryParseCoord(obj["lat"]?.ToString(), out var parsedLat)
                || !TryParseCoord(obj["lon"]?.ToString(), out var parsedLng))
            {
                parsedLat = lat;
                parsedLng = lng;
            }

            return new GeocodedPlace { Lat = parsedLat, Lng = parsedLng, Address = display! };
        }

        private async Task<List<GeocodeSuggestItem>> FetchSuggestAsync(
            string correlationId, string methodName, string query, int limit, string lang)
        {
            // Берём с запасом — отфильтруем не-населённые пункты
            var url = BuildSearchUrl(query, lang, limit: Math.Min(limit * 2, 20), addressDetails: true);
            var json = await GetNominatimJsonAsync(correlationId, methodName, url, "suggest");
            var arr = json as JArray;
            if (arr == null || arr.Count == 0)
                return new List<GeocodeSuggestItem>();

            var result = new List<GeocodeSuggestItem>();
            foreach (var token in arr)
            {
                if (token is not JObject obj)
                    continue;
                if (!IsLocalityHit(obj))
                    continue;

                var item = ParseSuggestHit(obj);
                if (item == null)
                    continue;

                result.Add(item);
                if (result.Count >= limit)
                    break;
            }

            return result;
        }

        private async Task<JToken?> GetNominatimJsonAsync(
            string correlationId, string methodName, string url, string mode)
        {
            await NominatimGate.WaitAsync().ConfigureAwait(false);
            try
            {
                var since = DateTimeOffset.UtcNow - _lastNominatimUtc;
                var waitMs = _minIntervalMs - (int)since.TotalMilliseconds;
                if (waitMs > 0)
                    await Task.Delay(waitMs).ConfigureAwait(false);

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.TryAddWithoutValidation("User-Agent", _userAgent);
                req.Headers.TryAddWithoutValidation("Accept", "application/json");

                logger.Debug(
                    correlationId,
                    null,
                    methodName,
                    "Nominatim geocode request",
                    null,
                    null,
                    new Dictionary<string, object> { ["mode"] = mode });

                using var cts = new CancellationTokenSource(_timeout);
                using var response = await Http.SendAsync(req, cts.Token).ConfigureAwait(false);
                _lastNominatimUtc = DateTimeOffset.UtcNow;

                if (!response.IsSuccessStatusCode)
                {
                    logger.Warn(correlationId, null, methodName,
                        $"Nominatim geocode HTTP {(int)response.StatusCode}");
                    return null;
                }

                var body = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(body))
                    return null;

                return JToken.Parse(body);
            }
            catch (OperationCanceledException)
            {
                logger.Warn(correlationId, null, methodName, "Nominatim geocode timeout");
                return null;
            }
            catch (Exception ex)
            {
                logger.Error(correlationId, null, methodName, "Nominatim geocode failed", null, ex);
                return null;
            }
            finally
            {
                NominatimGate.Release();
            }
        }

        private string BuildSearchUrl(string query, string lang, int limit, bool addressDetails)
        {
            var sb = new StringBuilder();
            sb.Append(_baseUrl.TrimEnd('/'));
            sb.Append("/search?format=jsonv2&limit=");
            sb.Append(limit.ToString(CultureInfo.InvariantCulture));
            sb.Append("&accept-language=");
            sb.Append(Uri.EscapeDataString(lang));
            sb.Append("&q=");
            sb.Append(Uri.EscapeDataString(query));
            if (addressDetails)
                sb.Append("&addressdetails=1");
            return sb.ToString();
        }

        private string BuildReverseUrl(double lat, double lng, string lang)
        {
            var sb = new StringBuilder();
            sb.Append(_baseUrl.TrimEnd('/'));
            sb.Append("/reverse?format=jsonv2&addressdetails=1&accept-language=");
            sb.Append(Uri.EscapeDataString(lang));
            sb.Append("&lat=");
            sb.Append(lat.ToString(CultureInfo.InvariantCulture));
            sb.Append("&lon=");
            sb.Append(lng.ToString(CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        private static GeocodedPlace? ParseSearchHit(JObject obj)
        {
            if (!TryParseCoord(obj["lat"]?.ToString(), out var lat)
                || !TryParseCoord(obj["lon"]?.ToString(), out var lng))
                return null;

            var address = obj["display_name"]?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(address))
                return null;

            return new GeocodedPlace { Lat = lat, Lng = lng, Address = address };
        }

        private static GeocodeSuggestItem? ParseSuggestHit(JObject obj)
        {
            if (!TryParseCoord(obj["lat"]?.ToString(), out var lat)
                || !TryParseCoord(obj["lon"]?.ToString(), out var lng))
                return null;

            var address = obj["address"] as JObject;
            var shortName = ExtractLocalityName(address)
                            ?? obj["name"]?.ToString()
                            ?? obj["display_name"]?.ToString();
            if (string.IsNullOrWhiteSpace(shortName))
                return null;

            var region = address?["state"]?.ToString()
                         ?? address?["region"]?.ToString()
                         ?? address?["county"]?.ToString()
                         ?? string.Empty;
            var country = address?["country"]?.ToString() ?? string.Empty;

            var suffix = string.Empty;
            if (!string.IsNullOrWhiteSpace(country)
                && !string.Equals(country, "Россия", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(country, "Russia", StringComparison.OrdinalIgnoreCase))
            {
                suffix = $", {country}";
            }
            else if (!string.IsNullOrWhiteSpace(region)
                     && !string.Equals(region, shortName, StringComparison.OrdinalIgnoreCase))
            {
                suffix = $", {region}";
            }

            return new GeocodeSuggestItem
            {
                Lat = lat,
                Lng = lng,
                ShortName = shortName!,
                Name = shortName + suffix
            };
        }

        private static bool IsLocalityHit(JObject obj)
        {
            var type = obj["type"]?.ToString() ?? string.Empty;
            var addresstype = obj["addresstype"]?.ToString() ?? string.Empty;
            var cls = obj["class"]?.ToString() ?? string.Empty;

            if (LocalityPlaceTypes.Contains(type) || LocalityPlaceTypes.Contains(addresstype))
                return true;

            // administrative с именем города в address
            if (string.Equals(cls, "boundary", StringComparison.OrdinalIgnoreCase)
                || string.Equals(cls, "place", StringComparison.OrdinalIgnoreCase)
                || string.Equals(cls, "boundary", StringComparison.OrdinalIgnoreCase))
            {
                return ExtractLocalityName(obj["address"] as JObject) != null;
            }

            return false;
        }

        private static string? ExtractLocalityName(JObject? address)
        {
            if (address == null)
                return null;

            foreach (var key in new[] { "city", "town", "village", "municipality", "hamlet", "suburb", "city_district" })
            {
                var v = address[key]?.ToString();
                if (!string.IsNullOrWhiteSpace(v))
                    return v;
            }

            return null;
        }

        private static bool TryParseCoord(string? raw, out double value)
            => double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

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
