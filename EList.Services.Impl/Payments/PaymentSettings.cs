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
            // Мастер-флаг ticketsEnabled гасит и платные продажи.
            if (!FeatureFlags.IsTicketsEnabled())
                return false;

            if (!ConfigurationManager.AppSettings.Contains("features:ticketSalesEnabled"))
                return true;

            return bool.TryParse(ConfigurationManager.AppSettings["features:ticketSalesEnabled"], out var flag)
                && flag;
        }

        /// <summary>
        /// Desk validate/check-in/undo: отдавать login/ФИО holder. Default false.
        /// </summary>
        public static bool IsTicketDeskRevealHolderEnabled()
        {
            if (!ConfigurationManager.AppSettings.Contains("features:ticketDeskRevealHolder"))
                return false;

            return bool.TryParse(ConfigurationManager.AppSettings["features:ticketDeskRevealHolder"], out var flag)
                && flag;
        }
    }

    public class TBankPaymentSettings
    {
        /// <summary>Прод-эквайринг. Тест по умолчанию в appsettings: rest-api-test.tinkoff.ru/v2.</summary>
        public const string DefaultApiBaseUrl = "https://securepay.tinkoff.ru/v2";

        /// <summary>Тестовый хост Init/GetState/Cancel (без суффикса метода; клиент добавит /Init).</summary>
        public const string TestApiBaseUrl = "https://rest-api-test.tinkoff.ru/v2";

        public const string DefaultSmRegisterBaseUrl = "https://register.tinkoff.ru/v1";

        /// <summary>Тестовый host регистрации точек Мультирасчётов (PDF «Регистрация…»).</summary>
        public const string DefaultAcqApiBaseUrl = "https://acqapi-test.tinkoff.ru";

        /// <summary>Боевой host acqapi (часто с mTLS + WL IP).</summary>
        public const string ProductionAcqApiBaseUrl = "https://acqapi.tinkoff.ru";

        /// <summary>Basic client id/secret по PDF (постоянные partner:partner на test и prod).</summary>
        public const string DefaultAcqApiClientId = "partner";
        public const string DefaultAcqApiClientSecret = "partner";

        public string TerminalKey { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ApiBaseUrl { get; set; } = TestApiBaseUrl;
        public string? NotificationUrl { get; set; }
        public string? SuccessUrl { get; set; }
        public string? FailUrl { get; set; }

        /// <summary>
        /// Ручной ShopCode для DEMO/локальных тестов без acqapi / SM-Register.
        /// </summary>
        public string? ManualShopCode { get; set; }

        /// <summary>
        /// Разрешить платные билеты без ShopCode (Init без Shops[] — вся сумма на терминал площадки).
        /// По умолчанию true на test host, false на securepay.
        /// </summary>
        public bool AllowTicketsWithoutShopCode { get; set; }

        /// <summary>Base URL A2C (<c>.../e2c/v2</c>). Пусто — вычисляется из <see cref="ApiBaseUrl"/>.</summary>
        public string? E2cApiBaseUrl { get; set; }

        /// <summary>
        /// После CONFIRMED билетного заказа сразу слать A2C продавцу (FinalPayout).
        /// Default true; выключить для ручных выплат / отладки.
        /// </summary>
        public bool AutoPayoutOnPaid { get; set; } = true;

        /// <summary>
        /// Срок жизни сделки (дни) для maintenance-воркера: после него — retry payout / closeSpDeal.
        /// Договор с банком уточняет фактический term; default 7.
        /// </summary>
        public int DealTermDays { get; set; } = 7;

        /// <summary>Целевой онбординг точек: acqapi OAuth + /sm-register/register.</summary>
        public TBankAcqApiSettings AcqApi { get; set; } = new();

        /// <summary>Legacy SM-Register (register.tinkoff.ru/v1) — fallback, если acqApi не настроен.</summary>
        public TBankSmRegisterSettings SmRegister { get; set; } = new();

        public static TBankPaymentSettings Load()
        {
            var settings = new TBankPaymentSettings
            {
                TerminalKey = Read("payments:tbank:terminalKey"),
                Password = Read("payments:tbank:password"),
                // Локально/staging по умолчанию — test host; прод задаёт securepay через appsettings/env.
                ApiBaseUrl = Read("payments:tbank:apiBaseUrl", TestApiBaseUrl),
                NotificationUrl = ReadNullable("payments:tbank:notificationUrl"),
                SuccessUrl = ReadNullable("payments:tbank:successUrl"),
                FailUrl = ReadNullable("payments:tbank:failUrl"),
                ManualShopCode = ReadNullable("payments:tbank:manualShopCode"),
                E2cApiBaseUrl = ReadNullable("payments:tbank:e2cApiBaseUrl"),
                AcqApi = new TBankAcqApiSettings
                {
                    BaseUrl = Read("payments:tbank:acqApi:baseUrl", DefaultAcqApiBaseUrl),
                    ClientId = Read("payments:tbank:acqApi:clientId", DefaultAcqApiClientId),
                    ClientSecret = Read("payments:tbank:acqApi:clientSecret", DefaultAcqApiClientSecret),
                    Username = Read("payments:tbank:acqApi:username"),
                    Password = Read("payments:tbank:acqApi:password"),
                    ClientCertificatePath = ReadNullable("payments:tbank:acqApi:clientCertificatePath"),
                    ClientCertificatePassword = ReadNullable("payments:tbank:acqApi:clientCertificatePassword")
                },
                SmRegister = new TBankSmRegisterSettings
                {
                    Username = Read("payments:tbank:smRegister:username"),
                    Password = Read("payments:tbank:smRegister:password"),
                    BaseUrl = Read("payments:tbank:smRegister:baseUrl", DefaultSmRegisterBaseUrl)
                }
            };

            if (string.IsNullOrWhiteSpace(settings.ApiBaseUrl))
                settings.ApiBaseUrl = TestApiBaseUrl;
            if (string.IsNullOrWhiteSpace(settings.AcqApi.BaseUrl))
                settings.AcqApi.BaseUrl = DefaultAcqApiBaseUrl;
            if (string.IsNullOrWhiteSpace(settings.AcqApi.ClientId))
                settings.AcqApi.ClientId = DefaultAcqApiClientId;
            if (string.IsNullOrWhiteSpace(settings.AcqApi.ClientSecret))
                settings.AcqApi.ClientSecret = DefaultAcqApiClientSecret;
            if (string.IsNullOrWhiteSpace(settings.SmRegister.BaseUrl))
                settings.SmRegister.BaseUrl = DefaultSmRegisterBaseUrl;
            var isTestHost = settings.ApiBaseUrl.Contains("rest-api-test", StringComparison.OrdinalIgnoreCase)
                || settings.ApiBaseUrl.Contains("DEMO", StringComparison.OrdinalIgnoreCase);
            settings.AllowTicketsWithoutShopCode = ReadBool(
                "payments:tbank:allowTicketsWithoutShopCode",
                defaultValue: isTestHost);
            settings.AutoPayoutOnPaid = ReadBool(
                "payments:tbank:autoPayoutOnPaid",
                defaultValue: true);
            settings.DealTermDays = ReadInt(
                "payments:tbank:dealTermDays",
                defaultValue: 7,
                min: 1,
                max: 365);

            return settings;
        }

        private static int ReadInt(string key, int defaultValue, int min, int max)
        {
            if (!ConfigurationManager.AppSettings.Contains(key)
                || string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings[key])
                || !int.TryParse(ConfigurationManager.AppSettings[key], out var value))
            {
                return defaultValue;
            }

            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        /// <summary>Базовый URL e2c API (<c>.../e2c/v2</c>).</summary>
        public string ResolveE2cApiBaseUrl()
        {
            if (!string.IsNullOrWhiteSpace(E2cApiBaseUrl))
                return E2cApiBaseUrl.Trim().TrimEnd('/');

            var api = (ApiBaseUrl ?? string.Empty).Trim().TrimEnd('/');
            if (api.Contains("rest-api-test", StringComparison.OrdinalIgnoreCase))
                return "https://rest-api-test.tinkoff.ru/e2c/v2";
            if (api.Contains("securepay", StringComparison.OrdinalIgnoreCase))
                return "https://securepay.tinkoff.ru/e2c/v2";
            if (api.EndsWith("/v2", StringComparison.OrdinalIgnoreCase))
                return api[..^3] + "/e2c/v2";
            return api + "/e2c/v2";
        }

        private static bool ReadBool(string key, bool defaultValue)
        {
            if (!ConfigurationManager.AppSettings.Contains(key)
                || string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings[key]))
                return defaultValue;

            return bool.TryParse(ConfigurationManager.AppSettings[key], out var flag) && flag;
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

    /// <summary>
    /// Настройки acqapi для регистрации точек Мультирасчётов.
    /// Ops: WL IP; на prod часто mTLS (clientCertificatePath).
    /// </summary>
    public class TBankAcqApiSettings
    {
        public string BaseUrl { get; set; } = TBankPaymentSettings.DefaultAcqApiBaseUrl;
        public string ClientId { get; set; } = TBankPaymentSettings.DefaultAcqApiClientId;
        public string ClientSecret { get; set; } = TBankPaymentSettings.DefaultAcqApiClientSecret;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? ClientCertificatePath { get; set; }
        public string? ClientCertificatePassword { get; set; }

        public bool IsConfigured()
            => !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
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
