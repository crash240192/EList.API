namespace EList.Models.Wallets
{
    /// <summary>Форматирование периода тарифа для UI / чека (как formatTariffPeriod на фронте).</summary>
    public static class TariffPeriodFormat
    {
        /// <summary>«2 мин.», «3 ч.», «3 ч. 15 мин.», «30 дн.»; пустой/нулевой → «30 дн.» (дефолт тарифа).</summary>
        public static string FormatLabel(TimeSpan period)
        {
            if (period <= TimeSpan.Zero)
                return "30 дн.";

            var totalMin = (int)Math.Ceiling(period.TotalMinutes);
            if (totalMin < 1)
                totalMin = 1;

            if (totalMin < 60)
                return $"{totalMin} мин.";

            if (totalMin < 24 * 60)
            {
                var hours = totalMin / 60;
                var minutes = totalMin % 60;
                return minutes > 0 ? $"{hours} ч. {minutes} мин." : $"{hours} ч.";
            }

            var days = (int)Math.Round(totalMin / (24.0 * 60.0));
            if (days < 1)
                days = 1;
            return $"{days} дн.";
        }

        /// <summary>Целые дни для legacy-полей API (короткий период → 1, не 0).</summary>
        public static int PeriodDaysForResponse(TimeSpan period)
        {
            if (period <= TimeSpan.Zero)
                return 30;
            return Math.Max(1, (int)Math.Ceiling(period.TotalDays));
        }
    }
}
