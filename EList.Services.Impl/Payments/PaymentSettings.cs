using EList.Common.Configuration;

namespace EList.Services.Impl.Payments
{
    /// <summary>
    /// Настройки платежей из appsettings:payments (+ env override).
    /// </summary>
    public class PaymentSettings
    {
        public string Provider { get; set; } = "yookassaStub";
        public decimal CommissionPercent { get; set; } = 10m;
        public string Currency { get; set; } = "RUB";
        public string ReturnUrl { get; set; } = "https://localhost/payments/return";
        public TBankPaymentSettings TBank { get; set; } = new();

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

            settings.TBank = TBankPaymentSettings.Load();
            return settings;
        }

        public bool IsTBankProvider()
            => string.Equals(Provider, "tbank", StringComparison.OrdinalIgnoreCase);

        public bool IsYooKassaStubProvider()
            => string.Equals(Provider, "yookassaStub", StringComparison.OrdinalIgnoreCase)
               || string.Equals(Provider, "yookassa", StringComparison.OrdinalIgnoreCase);

        public static bool IsTicketSalesGloballyEnabled()
        {
            if (!ConfigurationManager.AppSettings.Contains("features:ticketSalesEnabled"))
                return true;

            return bool.TryParse(ConfigurationManager.AppSettings["features:ticketSalesEnabled"], out var flag)
                && flag;
        }
    }

    public class TBankPaymentSettings
    {
        public const string DefaultApiBaseUrl = "https://securepay.tinkoff.ru/v2";
        public const string DefaultSmRegisterBaseUrl = "https://register.tinkoff.ru/v1";

        public string TerminalKey { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ApiBaseUrl { get; set; } = DefaultApiBaseUrl;
        public string? NotificationUrl { get; set; }
        public string? SuccessUrl { get; set; }
        public string? FailUrl { get; set; }

        /// <summary>
        /// Ручной ShopCode для DEMO/локальных тестов Init без SM-Register.
        /// </summary>
        public string? ManualShopCode { get; set; }

        public TBankSmRegisterSettings SmRegister { get; set; } = new();

        public static TBankPaymentSettings Load()
        {
            var settings = new TBankPaymentSettings
            {
                TerminalKey = Read("payments:tbank:terminalKey"),
                Password = Read("payments:tbank:password"),
                ApiBaseUrl = Read("payments:tbank:apiBaseUrl", DefaultApiBaseUrl),
                NotificationUrl = ReadNullable("payments:tbank:notificationUrl"),
                SuccessUrl = ReadNullable("payments:tbank:successUrl"),
                FailUrl = ReadNullable("payments:tbank:failUrl"),
                ManualShopCode = ReadNullable("payments:tbank:manualShopCode"),
                SmRegister = new TBankSmRegisterSettings
                {
                    Username = Read("payments:tbank:smRegister:username"),
                    Password = Read("payments:tbank:smRegister:password"),
                    BaseUrl = Read("payments:tbank:smRegister:baseUrl", DefaultSmRegisterBaseUrl)
                }
            };

            if (string.IsNullOrWhiteSpace(settings.ApiBaseUrl))
                settings.ApiBaseUrl = DefaultApiBaseUrl;
            if (string.IsNullOrWhiteSpace(settings.SmRegister.BaseUrl))
                settings.SmRegister.BaseUrl = DefaultSmRegisterBaseUrl;

            return settings;
        }

        public void EnsureAcquiringConfigured()
        {
            if (string.IsNullOrWhiteSpace(TerminalKey) || string.IsNullOrWhiteSpace(Password))
            {
                throw new InvalidOperationException(
                    "Т-Банк эквайринг не настроен: задайте payments__tbank__terminalKey и payments__tbank__password (env) или секцию payments:tbank.");
            }
        }

        private static string Read(string key, string defaultValue = "")
        {
            if (ConfigurationManager.AppSettings.Contains(key)
                && !string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings[key]))
            {
                return ConfigurationManager.AppSettings[key].Trim();
            }

            return defaultValue;
        }

        private static string? ReadNullable(string key)
        {
            if (ConfigurationManager.AppSettings.Contains(key)
                && !string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings[key]))
            {
                return ConfigurationManager.AppSettings[key].Trim();
            }

            return null;
        }
    }

    public class TBankSmRegisterSettings
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = TBankPaymentSettings.DefaultSmRegisterBaseUrl;

        public bool IsConfigured()
            => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
    }
}
