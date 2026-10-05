namespace EList.Services.Impl
{
    /// <summary>
    /// Сопоставление координат аккаунта с названием города (как в UI POPULAR_CITIES).
    /// </summary>
    internal static class ProfileCityResolver
    {
        private readonly record struct CityPoint(string Name, double Lat, double Lng);

        private static readonly CityPoint[] Cities =
        {
            new("Москва", 55.7558, 37.6173),
            new("Санкт-Петербург", 59.9311, 30.3609),
            new("Новосибирск", 54.9884, 82.9357),
            new("Екатеринбург", 56.8389, 60.6057),
            new("Казань", 55.8304, 49.0661),
            new("Нижний Новгород", 56.2965, 43.9361),
            new("Челябинск", 55.1644, 61.4368),
            new("Самара", 53.2028, 50.1408),
            new("Уфа", 54.7388, 55.9721),
            new("Ростов-на-Дону", 47.2357, 39.7015),
            new("Омск", 54.9885, 73.3242),
            new("Красноярск", 56.0153, 92.8932),
            new("Воронеж", 51.6683, 39.1919),
            new("Пермь", 58.0092, 56.2386),
            new("Волгоград", 48.7080, 44.5133),
            new("Краснодар", 45.0355, 38.9753),
            new("Саратов", 51.5336, 46.0343),
            new("Тюмень", 57.1522, 65.5272),
            new("Ижевск", 56.8526, 53.2068),
            new("Барнаул", 53.3606, 83.7636),
            new("Иркутск", 52.2978, 104.2964),
            new("Хабаровск", 48.4827, 135.0840),
            new("Владивосток", 43.1056, 131.8735),
            new("Ярославль", 57.6261, 39.8845),
            new("Томск", 56.4846, 84.9481),
        };

        public static string? ResolveNearestCityName(double? latitude, double? longitude)
        {
            if (latitude == null || longitude == null)
                return null;

            var lat = latitude.Value;
            var lng = longitude.Value;
            if (lat == 0 && lng == 0)
                return null;

            CityPoint nearest = Cities[0];
            var minDist = double.MaxValue;
            foreach (var city in Cities)
            {
                var dLat = city.Lat - lat;
                var dLng = city.Lng - lng;
                var dist = dLat * dLat + dLng * dLng;
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = city;
                }
            }

            return nearest.Name;
        }
    }
}
