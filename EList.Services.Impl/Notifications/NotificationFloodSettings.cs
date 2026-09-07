namespace EList.Services.Impl.Notifications
{
    /// <summary>
    /// Антифлуд для user-notifications. Читается из appsettings:notificationFlood.
    /// </summary>
    public class NotificationFloodSettings
    {
        public RatingFloodSettings Ratings { get; set; } = new();
        public ParticipationFloodSettings Participation { get; set; } = new();
        public RelatedSocialFloodSettings RelatedSocial { get; set; } = new();
        public EventUpdateFloodSettings EventUpdate { get; set; } = new();
    }

    public class RatingFloodSettings
    {
        /// <summary>Первые N оценок по событию уходят realtime организаторам.</summary>
        public int FirstRealtimeCount { get; set; } = 10;

        /// <summary>Окно агрегации digest после first-K, минуты.</summary>
        public int DigestWindowMinutes { get; set; } = 60;

        /// <summary>Оценки &lt;= порога всегда realtime (0 = выкл).</summary>
        public int LowScoreRealtimeMax { get; set; } = 2;
    }

    public class ParticipationFloodSettings
    {
        /// <summary>Первые N участников события — realtime организаторам; подписчики актора не режутся.</summary>
        public int FirstRealtimeCount { get; set; } = 20;

        public int DigestWindowMinutes { get; set; } = 60;
    }

    public class RelatedSocialFloodSettings
    {
        /// <summary>realtime | digest — режим RelatedPersonSubscribed/Unsubscribed.</summary>
        public string Mode { get; set; } = "realtime";

        public int DigestWindowMinutes { get; set; } = 60;
    }

    public class EventUpdateFloodSettings
    {
        /// <summary>Пушить update только при значимых полях (время/место/имя/active).</summary>
        public bool SignificantFieldsOnly { get; set; } = true;
    }

    public enum FloodDecision
    {
        SendRealtime = 0,
        Suppress = 1,
        SendDigest = 2
    }
}
