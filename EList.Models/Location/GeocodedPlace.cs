namespace EList.Models.Location
{
    /// <summary>
    /// Результат прямого/обратного геокодирования (совместим с UI GeocodedPlace).
    /// </summary>
    public class GeocodedPlace
    {
        /// <summary>Широта</summary>
        public double Lat { get; set; }

        /// <summary>Долгота</summary>
        public double Lng { get; set; }

        /// <summary>Адрес / текстовое описание объекта</summary>
        public string Address { get; set; } = string.Empty;
    }
}
