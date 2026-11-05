using EList.Common.Logger;
using EList.Common.Models;
using EList.Models.Location;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EList.Api.Controllers
{
    /// <summary>
    /// Геокодер Nominatim (OSM) через API (с серверным MemoryCache).
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("/api/geocode")]
    [LoggerHandlerWebApiFilter]
    public class GeocodeController : ControllerBase
    {
        private readonly IGeocodeService _geocodeService;

        public GeocodeController(IGeocodeService geocodeService)
        {
            _geocodeService = geocodeService ?? throw new ArgumentNullException(nameof(geocodeService));
        }

        /// <summary>
        /// Прямое геокодирование: адрес → координаты.
        /// </summary>
        /// <param name="q">Строка запроса</param>
        /// <param name="lang">Язык ответа (ru / en; по умолчанию ru)</param>
        /// <param name="cacheOnly">Только MemoryCache, без Nominatim (для UI: peek → fallback)</param>
        [AllowAnonymous]
        [HttpGet]
        public async Task<CommandResult<GeocodedPlace?>> GeocodeAsync(
            [FromQuery] string q,
            [FromQuery] string? lang = null,
            [FromQuery] bool cacheOnly = false)
        {
            return await _geocodeService.GeocodeAsync(q, lang, cacheOnly);
        }

        /// <summary>
        /// Обратное геокодирование: координаты → адрес.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("reverse")]
        public async Task<CommandResult<GeocodedPlace?>> ReverseGeocodeAsync(
            [FromQuery] double lat,
            [FromQuery] double lng,
            [FromQuery] string? kind = null,
            [FromQuery] string? lang = null,
            [FromQuery] bool cacheOnly = false)
        {
            return await _geocodeService.ReverseGeocodeAsync(lat, lng, kind, lang, cacheOnly);
        }

        /// <summary>
        /// Подсказки населённых пунктов (CitySearch).
        /// </summary>
        [AllowAnonymous]
        [HttpGet("suggest")]
        public async Task<CommandResult<List<GeocodeSuggestItem>>> SuggestAsync(
            [FromQuery] string q,
            [FromQuery] int limit = 8,
            [FromQuery] string? lang = null)
        {
            return await _geocodeService.SuggestAsync(q, limit, lang);
        }

        /// <summary>
        /// Прогрев кэша результатом с клиента.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("cache")]
        public async Task<CommandResult> WarmCacheAsync([FromBody] GeocodeCacheWarmRequest request)
        {
            return await _geocodeService.WarmCacheAsync(request);
        }
    }
}
