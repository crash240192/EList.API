using System.Security.Cryptography;
using System.Text;

namespace EList.Models.Wallets
{
    /// <summary>
    /// Человекочитаемые OrderId для тарифных платежей в эквайринге.
    /// Формат: <c>tariff:A7K9M2X3</c> (Crockford Base32, без I/L/O/U).
    /// Legacy: <c>wallet:{guid}</c> — всё ещё принимаем в webhook.
    /// </summary>
    public static class WalletDepositOrderIds
    {
        public const string TariffPrefix = "tariff:";
        public const string LegacyWalletPrefix = "wallet:";

        private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        private const int CodeLength = 8;

        public static string NewTariffOrderId()
        {
            return TariffPrefix + NewPublicCode();
        }

        public static string NewPublicCode()
        {
            var bytes = new byte[CodeLength];
            RandomNumberGenerator.Fill(bytes);
            var sb = new StringBuilder(CodeLength);
            for (var i = 0; i < CodeLength; i++)
                sb.Append(Alphabet[bytes[i] % Alphabet.Length]);
            return sb.ToString();
        }

        public static bool IsTariffOrderId(string? orderId)
        {
            return !string.IsNullOrWhiteSpace(orderId)
                && orderId.Trim().StartsWith(TariffPrefix, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsLegacyWalletOrderId(string? orderId)
        {
            return !string.IsNullOrWhiteSpace(orderId)
                && orderId.Trim().StartsWith(LegacyWalletPrefix, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsWalletContourOrderId(string? orderId)
        {
            return IsTariffOrderId(orderId) || IsLegacyWalletOrderId(orderId);
        }

        public static bool TryParseLegacyDepositId(string? orderId, out Guid depositId)
        {
            depositId = Guid.Empty;
            if (!IsLegacyWalletOrderId(orderId))
                return false;
            var raw = orderId!.Trim().Substring(LegacyWalletPrefix.Length).Trim();
            return Guid.TryParse(raw, out depositId);
        }
    }
}
