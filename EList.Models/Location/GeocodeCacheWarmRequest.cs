namespace EList.Models.Location
{
    /// <summary>
    /// Прогрев серверного кэша геокодера результатом, полученным на клиенте (Яндекс напрямую).
    /// </summary>
    public class GeocodeCacheWarmRequest
    {
        /// <summary>Прямой запрос (fwd). Нормализуется так же, как в GeocodeAsync.</summary>
        public string? Query { get; set; }

        /// <summary>Обратное: широта</summary>
        public double? Lat { get; set; }

        /// <summary>Обратное: долгота</summary>
        public double? Lng { get; set; }

        /// <summary>Обратное: kind Яндекса</summary>
        public string? Kind { get; set; }

        /// <summary>Язык ключа кэша (по умолчанию ru_RU)</summary>
        public string? Lang { get; set; }

        /// <summary>Значение для кэша</summary>
        public GeocodedPlace Place { get; set; } = null!;
    }
}
