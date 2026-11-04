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
        [AllowAnonymous]
        [HttpGet]
        public async Task<CommandResult<GeocodedPlace?>> GeocodeAsync(
            [FromQuery] string q,
            [FromQuery] string? lang = null)
        {
            return await _geocodeService.GeocodeAsync(q, lang);
        }

        /// <summary>
        /// Обратное геокодирование: координаты → адрес.
        /// </summary>
        /// <param name="lat">Широта</param>
        /// <param name="lng">Долгота</param>
        /// <param name="kind">Опциональный kind Яндекса (например locality)</param>
        /// <param name="lang">Язык ответа</param>
        [AllowAnonymous]
        [HttpGet("reverse")]
        public async Task<CommandResult<GeocodedPlace?>> ReverseGeocodeAsync(
            [FromQuery] double lat,
            [FromQuery] double lng,
            [FromQuery] string? kind = null,
            [FromQuery] string? lang = null)
        {
            return await _geocodeService.ReverseGeocodeAsync(lat, lng, kind, lang);
        }
    }
}
