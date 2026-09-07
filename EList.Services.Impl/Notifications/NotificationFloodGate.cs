using System.Collections.Concurrent;
using EList.Common.Configuration;

namespace EList.Services.Impl.Notifications
{
    /// <summary>
    /// In-memory first-K / digest gate для уведомлений (single-instance).
    /// </summary>
    public class NotificationFloodGate
    {
        private readonly ConcurrentDictionary<string, DigestBucket> _buckets = new();

        public NotificationFloodSettings GetSettings()
        {
            var settings = new NotificationFloodSettings();
            TryBindInt("notificationFlood:ratings:firstRealtimeCount", v => settings.Ratings.FirstRealtimeCount = v);
            TryBindInt("notificationFlood:ratings:digestWindowMinutes", v => settings.Ratings.DigestWindowMinutes = v);
            TryBindInt("notificationFlood:ratings:lowScoreRealtimeMax", v => settings.Ratings.LowScoreRealtimeMax = v);

            TryBindInt("notificationFlood:participation:firstRealtimeCount", v => settings.Participation.FirstRealtimeCount = v);
            TryBindInt("notificationFlood:participation:digestWindowMinutes", v => settings.Participation.DigestWindowMinutes = v);

            if (ConfigurationManager.AppSettings.Contains("notificationFlood:relatedSocial:mode"))
            {
                var mode = ConfigurationManager.AppSettings["notificationFlood:relatedSocial:mode"];
                if (!string.IsNullOrWhiteSpace(mode))
                    settings.RelatedSocial.Mode = mode.Trim();
            }

            TryBindInt("notificationFlood:relatedSocial:digestWindowMinutes", v => settings.RelatedSocial.DigestWindowMinutes = v);

            if (ConfigurationManager.AppSettings.Contains("notificationFlood:eventUpdate:significantFieldsOnly")
                && bool.TryParse(ConfigurationManager.AppSettings["notificationFlood:eventUpdate:significantFieldsOnly"], out var significant))
            {
                settings.EventUpdate.SignificantFieldsOnly = significant;
            }

            return settings;
        }

        /// <summary>
        /// first-K по счётчику сущности (рейтинги / участники), затем digest по окну.
        /// </summary>
        public FloodDecision EvaluateFirstKThenDigest(
            string bucketKey,
            int entityCountAfterEvent,
            int firstRealtimeCount,
            int digestWindowMinutes,
            out int pendingCount)
        {
            pendingCount = 0;
            if (firstRealtimeCount < 0)
                firstRealtimeCount = 0;
            if (digestWindowMinutes <= 0)
                digestWindowMinutes = 60;

            if (entityCountAfterEvent <= firstRealtimeCount)
                return FloodDecision.SendRealtime;

            return AccumulateDigest(bucketKey, digestWindowMinutes, out pendingCount);
        }

        /// <summary>
        /// Режим related social: realtime всегда, digest — только агрегаты.
        /// </summary>
        public FloodDecision EvaluateMode(
            string bucketKey,
            string mode,
            int digestWindowMinutes,
            out int pendingCount)
        {
            pendingCount = 0;
            if (!string.Equals(mode, "digest", StringComparison.OrdinalIgnoreCase))
                return FloodDecision.SendRealtime;

            if (digestWindowMinutes <= 0)
                digestWindowMinutes = 60;

            return AccumulateDigest(bucketKey, digestWindowMinutes, out pendingCount);
        }

        private FloodDecision AccumulateDigest(string bucketKey, int digestWindowMinutes, out int pendingCount)
        {
            var now = DateTimeOffset.UtcNow;
            var bucket = _buckets.AddOrUpdate(
                bucketKey,
                _ => new DigestBucket { Count = 1, WindowStart = now },
                (_, existing) =>
                {
                    existing.Count++;
                    return existing;
                });

            pendingCount = bucket.Count;
            var window = TimeSpan.FromMinutes(digestWindowMinutes);

            // Первое событие после first-K только открывает окно; flush — по истечении окна.
            if (bucket.LastFlushedAt == null && bucket.Count == 1)
                return FloodDecision.Suppress;

            var anchor = bucket.LastFlushedAt ?? bucket.WindowStart;
            if (now - anchor < window)
                return FloodDecision.Suppress;

            pendingCount = bucket.Count;
            bucket.Count = 0;
            bucket.LastFlushedAt = now;
            bucket.WindowStart = now;
            return FloodDecision.SendDigest;
        }

        private static void TryBindInt(string key, Action<int> apply)
        {
            if (!ConfigurationManager.AppSettings.Contains(key))
                return;
            if (int.TryParse(ConfigurationManager.AppSettings[key], out var value) && value >= 0)
                apply(value);
        }

        private sealed class DigestBucket
        {
            public int Count;
            public DateTimeOffset WindowStart;
            public DateTimeOffset? LastFlushedAt;
        }
    }
}
