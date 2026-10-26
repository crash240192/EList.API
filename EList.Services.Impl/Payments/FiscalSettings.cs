using EList.Common.Configuration;

namespace EList.Services.Impl.Payments
{
    /// <summary>
    /// Прямая фискализация через CloudKassir API (<c>kkt/receipt</c>), в обход Init.Receipt Т‑Банка.
    /// Default: выключено. На проде с кассой на терминале использовать Init.Receipt, не этот контур.
    /// Config: <c>cloudKassir:directFiscalization:*</c>
    /// </summary>
    public class FiscalSettings
    {
        public const string DefaultCloudKassirApiBaseUrl = "https://api.cloudpayments.ru";

        public CloudKassirDirectFiscalizationSettings DirectFiscalization { get; set; } = new();

        public static FiscalSettings Load()
        {
            const string prefix = "cloudKassir:directFiscalization:";
            var settings = new FiscalSettings
            {
                DirectFiscalization = new CloudKassirDirectFiscalizationSettings
                {
                    Enabled = ReadBool($"{prefix}enabled", defaultValue: false),
                    PublicId = Read($"{prefix}publicId"),
                    ApiSecret = Read($"{prefix}apiSecret"),
                    ApiBaseUrl = Read($"{prefix}apiBaseUrl", DefaultCloudKassirApiBaseUrl),
                    // ИНН владельца ККТ (площадка / агент), не ИНН продавца билета.
                    Inn = Read($"{prefix}inn"),
                    TaxationSystem = ReadInt($"{prefix}taxationSystem", defaultValue: 0, min: 0, max: 5),
                    Vat = Read($"{prefix}vat", "null"),
                    AgentSign = ReadInt($"{prefix}agentSign", defaultValue: 6, min: 0, max: 6)
                }
            };

            if (string.IsNullOrWhiteSpace(settings.DirectFiscalization.ApiBaseUrl))
                settings.DirectFiscalization.ApiBaseUrl = DefaultCloudKassirApiBaseUrl;

            return settings;
        }

        /// <summary>Прямой CloudKassir включён и сконфигурирован.</summary>
        public bool IsDirectFiscalizationEnabled()
            => DirectFiscalization.Enabled && DirectFiscalization.IsConfigured();

        private static string Read(string key, string defaultValue = "")
        {
            if (ConfigurationManager.AppSettings.Contains(key)
                && !string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings[key]))
            {
                return ConfigurationManager.AppSettings[key].Trim();
            }

            return defaultValue;
        }

        private static bool ReadBool(string key, bool defaultValue)
        {
            if (!ConfigurationManager.AppSettings.Contains(key)
                || string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings[key]))
                return defaultValue;

            return bool.TryParse(ConfigurationManager.AppSettings[key], out var flag) && flag;
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
    }

    public class CloudKassirDirectFiscalizationSettings
    {
        public bool Enabled { get; set; }

        public string PublicId { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public string ApiBaseUrl { get; set; } = FiscalSettings.DefaultCloudKassirApiBaseUrl;

        /// <summary>ИНН организации/ИП, на который зарегистрирована ККТ (владелец кассы).</summary>
        public string Inn { get; set; } = string.Empty;

        /// <summary>Система налогообложения (0–5), см. CloudKassir docs.</summary>
        public int TaxationSystem { get; set; }

        /// <summary>Ставка НДС: null / 0 / 10 / 20 / 110 / 120 …</summary>
        public string Vat { get; set; } = "null";

        /// <summary>Признак агента по предмету расчёта (6 = «Агент»).</summary>
        public int AgentSign { get; set; } = 6;

        public bool IsConfigured()
            => !string.IsNullOrWhiteSpace(PublicId)
               && !string.IsNullOrWhiteSpace(ApiSecret)
               && !string.IsNullOrWhiteSpace(Inn);
    }
}
