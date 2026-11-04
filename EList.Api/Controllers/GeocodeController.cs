using EList.Common.Logger;
using EList.Common.Models;
using EList.Models.Location;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EList.Api.Controllers
{
    /// <summary>
    /// Геокодер Яндекса через API (с серверным MemoryCache).
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
        /// <param name="lang">Язык ответа Яндекса (по умолчанию ru_RU)</param>
        /// <param name="cacheOnly">Только MemoryCache, без Яндекса (для UI: peek → fallback)</param>
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
        /// Прогрев кэша результатом с клиента (после прямого вызова Яндекса).
        /// </summary>
        [AllowAnonymous]
        [HttpPost("cache")]
        public async Task<CommandResult> WarmCacheAsync([FromBody] GeocodeCacheWarmRequest request)
        {
            return await _geocodeService.WarmCacheAsync(request);
        }
    }
}
