using EList.Common.Configuration;

namespace EList.Services.Impl.Payments
{
    /// <summary>
    /// Настройки фискализации (CloudKassir). Default: выключено.
    /// </summary>
    public class FiscalSettings
    {
        public const string DefaultCloudKassirApiBaseUrl = "https://api.cloudpayments.ru";

        /// <summary><c>none</c> \| <c>cloudKassir</c></summary>
        public string Provider { get; set; } = "none";

        public bool Enabled { get; set; }

        public CloudKassirSettings CloudKassir { get; set; } = new();

        public static FiscalSettings Load()
        {
            var settings = new FiscalSettings
            {
                Provider = Read("fiscal:provider", "none"),
                Enabled = ReadBool("fiscal:enabled", defaultValue: false),
                CloudKassir = new CloudKassirSettings
                {
                    PublicId = Read("fiscal:cloudKassir:publicId"),
                    ApiSecret = Read("fiscal:cloudKassir:apiSecret"),
                    ApiBaseUrl = Read("fiscal:cloudKassir:apiBaseUrl", DefaultCloudKassirApiBaseUrl),
                    TaxationSystem = ReadInt("fiscal:cloudKassir:taxationSystem", defaultValue: 0, min: 0, max: 5),
                    Vat = Read("fiscal:cloudKassir:vat", "null"),
                    AgentSign = ReadInt("fiscal:cloudKassir:agentSign", defaultValue: 6, min: 0, max: 6)
                }
            };

            if (string.IsNullOrWhiteSpace(settings.CloudKassir.ApiBaseUrl))
                settings.CloudKassir.ApiBaseUrl = DefaultCloudKassirApiBaseUrl;

            return settings;
        }

        public bool IsCloudKassir()
            => Enabled
               && string.Equals(Provider, "cloudKassir", StringComparison.OrdinalIgnoreCase)
               && CloudKassir.IsConfigured();

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

    public class CloudKassirSettings
    {
        public string PublicId { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public string ApiBaseUrl { get; set; } = FiscalSettings.DefaultCloudKassirApiBaseUrl;

        /// <summary>Система налогообложения (0–5), см. CloudKassir docs.</summary>
        public int TaxationSystem { get; set; }

        /// <summary>Ставка НДС: null / 0 / 10 / 20 / 110 / 120 …</summary>
        public string Vat { get; set; } = "null";

        /// <summary>Признак агента (6 = another agent / уточнить по кассе).</summary>
        public int AgentSign { get; set; } = 6;

        public bool IsConfigured()
            => !string.IsNullOrWhiteSpace(PublicId) && !string.IsNullOrWhiteSpace(ApiSecret);
    }
}
