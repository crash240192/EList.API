using EList.Common.Configuration;

namespace EList.Services.Impl.AbuseProtection
{
    /// <summary>
    /// Hard safety caps for event creation / search (independent of tariff marketing limits).
    /// Read from appsettings section abuseProtection.
    /// </summary>
    public sealed class AbuseProtectionOptions
    {
        public AbuseProtectionOptions()
        {
            Enabled = ReadBool("abuseProtection:enabled", true);
            MaxCreatesPerMinute = ReadInt("abuseProtection:maxCreatesPerMinute", 5);
            MaxCreatesPerHour = ReadInt("abuseProtection:maxCreatesPerHour", 30);
            MaxCreatesPerDay = ReadInt("abuseProtection:maxCreatesPerDay", 50);
            SafetyMaxActiveEvents = ReadInt("abuseProtection:safetyMaxActiveEvents", 200);
            GeoSpamRadiusMeters = ReadDouble("abuseProtection:geoSpamRadiusMeters", 80);
            MaxEventsNearLocationPerDay = ReadInt("abuseProtection:maxEventsNearLocationPerDay", 5);
            SearchMaxPageSize = ReadInt("abuseProtection:searchMaxPageSize", 50);
        }

        public bool Enabled { get; set; }

        /// <summary>Max create-event calls per account per rolling minute.</summary>
        public int MaxCreatesPerMinute { get; set; }

        /// <summary>Max create-event calls per account per rolling hour.</summary>
        public int MaxCreatesPerHour { get; set; }

        /// <summary>Max events created per account (or org) in last 24h, regardless of tariff.</summary>
        public int MaxCreatesPerDay { get; set; }

        /// <summary>
        /// Cap on active (not ended/cancelled) events when tariff MaxEventsCount is null.
        /// </summary>
        public int SafetyMaxActiveEvents { get; set; }

        /// <summary>Radius in meters for near-duplicate geo spam check.</summary>
        public double GeoSpamRadiusMeters { get; set; }

        /// <summary>Max events from same organizer near same point within 24h.</summary>
        public int MaxEventsNearLocationPerDay { get; set; }

        /// <summary>Hard cap for events search pageSize.</summary>
        public int SearchMaxPageSize { get; set; }

        private static bool ReadBool(string key, bool fallback)
        {
            if (ConfigurationManager.AppSettings.Contains(key)
                && bool.TryParse(ConfigurationManager.AppSettings[key], out var v))
                return v;
            return fallback;
        }

        private static int ReadInt(string key, int fallback)
        {
            if (ConfigurationManager.AppSettings.Contains(key)
                && int.TryParse(ConfigurationManager.AppSettings[key], out var v)
                && v > 0)
                return v;
            return fallback;
        }

        private static double ReadDouble(string key, double fallback)
        {
            if (ConfigurationManager.AppSettings.Contains(key)
                && double.TryParse(ConfigurationManager.AppSettings[key],
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var v)
                && v > 0)
                return v;
            return fallback;
        }
    }
}
