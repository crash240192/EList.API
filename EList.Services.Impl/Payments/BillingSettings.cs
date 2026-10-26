using EList.Common.Configuration;

namespace EList.Services.Impl.Payments
{
    /// <summary>
    /// Режим биллинга тарифного контура.
    /// PeriodPurchase — покупка периода без автопродления (целевой).
    /// WalletAutoRenew — legacy: баланс + DebtCollector.
    /// </summary>
    public static class BillingSettings
    {
        public const string ModePeriodPurchase = "PeriodPurchase";
        public const string ModeWalletAutoRenew = "WalletAutoRenew";

        public static string Mode
        {
            get
            {
                if (ConfigurationManager.AppSettings.Contains("billing:mode")
                    && !string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["billing:mode"]))
                {
                    return ConfigurationManager.AppSettings["billing:mode"].Trim();
                }

                // Default: новый флоу.
                return ModePeriodPurchase;
            }
        }

        public static bool IsPeriodPurchase =>
            string.Equals(Mode, ModePeriodPurchase, StringComparison.OrdinalIgnoreCase);

        public static bool IsWalletAutoRenew =>
            string.Equals(Mode, ModeWalletAutoRenew, StringComparison.OrdinalIgnoreCase);
    }
}
