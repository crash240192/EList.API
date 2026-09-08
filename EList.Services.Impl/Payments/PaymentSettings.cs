using EList.Common.Configuration;

namespace EList.Services.Impl.Payments
{
    /// <summary>
    /// Настройки платежей из appsettings:payments.
    /// </summary>
    public class PaymentSettings
    {
        public string Provider { get; set; } = "yookassaStub";
        public decimal CommissionPercent { get; set; } = 10m;
        public string Currency { get; set; } = "RUB";
        public string ReturnUrl { get; set; } = "https://localhost/payments/return";

        public static PaymentSettings Load()
        {
            var settings = new PaymentSettings();

            if (ConfigurationManager.AppSettings.Contains("payments:provider")
                && !string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["payments:provider"]))
            {
                settings.Provider = ConfigurationManager.AppSettings["payments:provider"].Trim();
            }

            if (ConfigurationManager.AppSettings.Contains("payments:commissionPercent")
                && decimal.TryParse(
                    ConfigurationManager.AppSettings["payments:commissionPercent"],
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var percent)
                && percent >= 0 && percent <= 100)
            {
                settings.CommissionPercent = percent;
            }

            if (ConfigurationManager.AppSettings.Contains("payments:currency")
                && !string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["payments:currency"]))
            {
                settings.Currency = ConfigurationManager.AppSettings["payments:currency"].Trim().ToUpperInvariant();
            }

            if (ConfigurationManager.AppSettings.Contains("payments:returnUrl")
                && !string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["payments:returnUrl"]))
            {
                settings.ReturnUrl = ConfigurationManager.AppSettings["payments:returnUrl"].Trim();
            }

            return settings;
        }

        public static bool IsTicketSalesGloballyEnabled()
        {
            if (!ConfigurationManager.AppSettings.Contains("features:ticketSalesEnabled"))
                return true;

            return bool.TryParse(ConfigurationManager.AppSettings["features:ticketSalesEnabled"], out var flag)
                && flag;
        }
    }
}
