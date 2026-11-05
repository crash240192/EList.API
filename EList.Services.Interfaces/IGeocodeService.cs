using EList.Common.Models;
using EList.Models.Location;

namespace EList.Services.Interfaces
{
    /// <summary>
    /// Геокодер (Nominatim / OSM) с in-process кэшем (MemoryCache).
    /// </summary>
    public interface IGeocodeService
    {
        /// <summary>
        /// Прямое геокодирование: строка адреса → координаты.
        /// </summary>
        /// <param name="cacheOnly">Только чтение MemoryCache, без обращения к провайдеру.</param>
        Task<CommandResult<GeocodedPlace?>> GeocodeAsync(string query, string? lang = null, bool cacheOnly = false);

        /// <summary>
        /// Обратное геокодирование: координаты → адрес.
        /// </summary>
        /// <param name="kind">Опциональный семантический фильтр (например locality) — влияет на ключ кэша и формат ответа.</param>
        /// <param name="cacheOnly">Только чтение MemoryCache, без обращения к провайдеру.</param>
        Task<CommandResult<GeocodedPlace?>> ReverseGeocodeAsync(
            double lat, double lng, string? kind = null, string? lang = null, bool cacheOnly = false);

        /// <summary>
        /// Подсказки населённых пунктов по строке (для выбора города).
        /// </summary>
        Task<CommandResult<List<GeocodeSuggestItem>>> SuggestAsync(
            string query, int limit = 8, string? lang = null);

        /// <summary>
        /// Положить в кэш результат, уже полученный клиентом (без повторного HTTP к провайдеру).
        /// </summary>
        Task<CommandResult> WarmCacheAsync(GeocodeCacheWarmRequest request);
    }
}
