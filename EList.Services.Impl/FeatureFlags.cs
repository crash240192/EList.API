using EList.Common.Configuration;

namespace EList.Services.Impl
{
    /// <summary>
    /// Глобальные kill-switch'и продукта (appsettings features:* / env features__*).
    /// </summary>
    public static class FeatureFlags
    {
        public const string OrganizationsDisabledMessage =
            "Функционал организаций временно отключён";

        public const string TicketsDisabledMessage =
            "Функционал билетов временно отключён";

        /// <summary>
        /// Создание организаций и сбор данных (legal/payout/verification/onboarding/CanSellTickets).
        /// Default false — безопасный запуск без агентской схемы.
        /// </summary>
        public static bool IsOrganizationsEnabled()
            => ReadBool("features:organizationsEnabled", defaultValue: false);

        /// <summary>
        /// Весь билетный контур: заказы, типы, desk, check-in, MyTickets, ticketsEnabled на событии.
        /// Default false. При false ticketSalesEnabled тоже считается выключенным для UI.
        /// </summary>
        public static bool IsTicketsEnabled()
            => ReadBool("features:ticketsEnabled", defaultValue: false);

        private static bool ReadBool(string key, bool defaultValue)
        {
            if (!ConfigurationManager.AppSettings.Contains(key)
                || string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings[key]))
                return defaultValue;

            return bool.TryParse(ConfigurationManager.AppSettings[key], out var flag) && flag;
        }
    }
}
