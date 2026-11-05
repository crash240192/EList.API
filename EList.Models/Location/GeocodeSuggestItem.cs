namespace EList.Models.Location
{
    /// <summary>
    /// Подсказка геокодера (города / населённые пункты) для UI CitySearch.
    /// </summary>
    public class GeocodeSuggestItem
    {
        public double Lat { get; set; }
        public double Lng { get; set; }

        /// <summary>Подпись в списке (город + регион/страна).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Короткое имя для поля ввода.</summary>
        public string ShortName { get; set; } = string.Empty;
    }
}
