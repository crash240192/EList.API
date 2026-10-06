using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace EList.Services.Impl.Payments.TBank
{
    /// <summary>
    /// Подпись Token для API Т-Банка: корневые скаляры + Password, сортировка по ключу, SHA-256 hex.
    /// </summary>
    public static class TBankTokenSigner
    {
        public static string Sign(IDictionary<string, object?> values, string password)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password is required", nameof(password));

            var pairs = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, value) in values)
            {
                if (string.IsNullOrWhiteSpace(key)
                    || string.Equals(key, "Token", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(key, "Password", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!TryFormatScalar(value, out var formatted))
                    continue;

                pairs[key] = formatted;
            }

            pairs["Password"] = password;

            var concatenated = string.Concat(pairs.Values);
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(concatenated));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        /// <summary>Подпись / проверка Token для произвольного JSON-объекта (Init, webhook).</summary>
        public static string SignJsonObject(JObject payload, string password)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            var values = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var property in payload.Properties())
            {
                if (property.Value == null || property.Value.Type == JTokenType.Null)
                    continue;

                if (property.Value.Type is JTokenType.Object or JTokenType.Array)
                    continue;

                values[property.Name] = property.Value.Type switch
                {
                    JTokenType.Boolean => property.Value.Value<bool>(),
                    JTokenType.Integer => property.Value.Value<long>(),
                    JTokenType.Float => property.Value.Value<decimal>(),
                    _ => property.Value.ToString()
                };
            }

            return Sign(values, password);
        }

        public static bool VerifyJsonObject(JObject payload, string password, string? expectedToken)
        {
            if (string.IsNullOrWhiteSpace(expectedToken))
                return false;

            var actual = SignJsonObject(payload, password);
            return string.Equals(actual, expectedToken.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryFormatScalar(object? value, out string formatted)
        {
            formatted = string.Empty;
            if (value == null)
                return false;

            switch (value)
            {
                case string s:
                    if (string.IsNullOrEmpty(s))
                        return false;
                    formatted = s;
                    return true;
                case bool b:
                    formatted = b ? "true" : "false";
                    return true;
                case byte or sbyte or short or ushort or int or uint or long or ulong:
                    formatted = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                    return formatted.Length > 0;
                case float or double or decimal:
                    formatted = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                    return formatted.Length > 0;
                case Enum e:
                    formatted = Convert.ToString(Convert.ChangeType(e, Enum.GetUnderlyingType(e.GetType()), CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)
                        ?? e.ToString();
                    return formatted.Length > 0;
                default:
                    return false;
            }
        }
    }
}
