namespace EList.Models.Media
{
    /// <summary>
    /// Порядок групп мероприятий в GET /api/media/albums/byEvents.
    /// </summary>
    public static class EventAlbumsSort
    {
        public const string Date = "date";
        public const string LatestPhotos = "latestPhotos";
        public const string Name = "name";

        /// <summary>
        /// Неизвестный / пустой → date (историческое поведение API: по StartTime ASC).
        /// </summary>
        public static string Normalize(string? sort)
        {
            if (string.IsNullOrWhiteSpace(sort))
                return Date;

            if (string.Equals(sort, LatestPhotos, StringComparison.OrdinalIgnoreCase))
                return LatestPhotos;
            if (string.Equals(sort, Name, StringComparison.OrdinalIgnoreCase))
                return Name;
            if (string.Equals(sort, Date, StringComparison.OrdinalIgnoreCase))
                return Date;

            return Date;
        }
    }
}
