using EList.Common.Models;
using EList.Models.Location;

namespace EList.Services.Interfaces
{
    /// <summary>
    /// Геокодер Яндекса с in-process кэшем (MemoryCache).
    /// </summary>
    public interface IGeocodeService
    {
        /// <summary>
        /// Прямое геокодирование: строка адреса → координаты.
        /// </summary>
        /// <param name="cacheOnly">Только чтение MemoryCache, без обращения в Яндекс.</param>
        Task<CommandResult<GeocodedPlace?>> GeocodeAsync(string query, string? lang = null, bool cacheOnly = false);

        /// <summary>
        /// Обратное геокодирование: координаты → адрес.
        /// </summary>
        /// <param name="kind">Опциональный фильтр Яндекса (например locality).</param>
        /// <param name="cacheOnly">Только чтение MemoryCache, без обращения в Яндекс.</param>
        Task<CommandResult<GeocodedPlace?>> ReverseGeocodeAsync(
            double lat, double lng, string? kind = null, string? lang = null, bool cacheOnly = false);

        /// <summary>
        /// Положить в кэш результат, уже полученный клиентом у Яндекса (без повторного HTTP к Яндексу).
        /// </summary>
        Task<CommandResult> WarmCacheAsync(GeocodeCacheWarmRequest request);
    }
}
