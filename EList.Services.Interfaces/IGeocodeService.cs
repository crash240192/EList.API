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
        Task<CommandResult<GeocodedPlace?>> GeocodeAsync(string query, string? lang = null);

        /// <summary>
        /// Обратное геокодирование: координаты → адрес.
        /// </summary>
        /// <param name="kind">Опциональный фильтр Яндекса (например locality).</param>
        Task<CommandResult<GeocodedPlace?>> ReverseGeocodeAsync(double lat, double lng, string? kind = null, string? lang = null);
    }
}
