using System.Security.Cryptography;
using System.Text;
using EList.Common.Configuration;

namespace EList.DbDataProvider.Security
{
    /// <summary>
    /// AES-256-GCM для хранения + HMAC-SHA256 для blind-index.
    /// Ключи берутся из appsettings: encryption:fieldKey (base64, 32 байта)
    /// либо выводятся из encryption:salt через PBKDF2.
    /// </summary>
    public class FieldEncryptor : IFieldEncryptor
    {
        public const string CipherPrefix = "e1.";

        private readonly byte[] _aesKey;
        private readonly byte[] _hmacKey;

        public FieldEncryptor()
        {
            var salt = ConfigurationManager.AppSettings.Contains("encryption:salt")
                ? ConfigurationManager.AppSettings["encryption:salt"] ?? string.Empty
                : string.Empty;

            if (ConfigurationManager.AppSettings.Contains("encryption:fieldKey")
                && !string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["encryption:fieldKey"]))
            {
                _aesKey = Convert.FromBase64String(ConfigurationManager.AppSettings["encryption:fieldKey"]);
                if (_aesKey.Length != 32)
                    throw new InvalidOperationException("encryption:fieldKey must be 32 bytes (base64-encoded AES-256 key)");
            }
            else
            {
                _aesKey = DeriveKey(salt, "elist-field-aes-v1", 32);
            }

            if (ConfigurationManager.AppSettings.Contains("encryption:indexKey")
                && !string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["encryption:indexKey"]))
            {
                _hmacKey = Convert.FromBase64String(ConfigurationManager.AppSettings["encryption:indexKey"]);
            }
            else
            {
                // По умолчанию — тот же salt из appsettings (как обсуждали)
                _hmacKey = DeriveKey(salt, "elist-field-hmac-v1", 32);
            }
        }

        public string? Encrypt(string? plaintext)
        {
            if (string.IsNullOrEmpty(plaintext))
                return plaintext;

            if (IsEncrypted(plaintext))
                return plaintext;

            var nonce = new byte[12];
            RandomNumberGenerator.Fill(nonce);

            var plainBytes = Encoding.UTF8.GetBytes(plaintext);
            var cipher = new byte[plainBytes.Length];
            var tag = new byte[16];

            using (var aes = new AesGcm(_aesKey))
            {
                aes.Encrypt(nonce, plainBytes, cipher, tag);
            }

            var payload = new byte[nonce.Length + cipher.Length + tag.Length];
            Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
            Buffer.BlockCopy(cipher, 0, payload, nonce.Length, cipher.Length);
            Buffer.BlockCopy(tag, 0, payload, nonce.Length + cipher.Length, tag.Length);

            return CipherPrefix + Convert.ToBase64String(payload);
        }

        public string? Decrypt(string? stored)
        {
            if (string.IsNullOrEmpty(stored))
                return stored;

            if (!IsEncrypted(stored))
                return stored;

            try
            {
                var payload = Convert.FromBase64String(stored.Substring(CipherPrefix.Length));
                if (payload.Length < 12 + 16)
                    return stored;

                var nonce = new byte[12];
                var tag = new byte[16];
                var cipherLen = payload.Length - 12 - 16;
                var cipher = new byte[cipherLen];

                Buffer.BlockCopy(payload, 0, nonce, 0, 12);
                Buffer.BlockCopy(payload, 12, cipher, 0, cipherLen);
                Buffer.BlockCopy(payload, 12 + cipherLen, tag, 0, 16);

                var plain = new byte[cipherLen];
                using (var aes = new AesGcm(_aesKey))
                {
                    aes.Decrypt(nonce, cipher, tag, plain);
                }

                return Encoding.UTF8.GetString(plain);
            }
            catch (CryptographicException)
            {
                return stored;
            }
        }

        public bool IsEncrypted(string? stored)
        {
            return !string.IsNullOrEmpty(stored)
                && stored.StartsWith(CipherPrefix, StringComparison.Ordinal);
        }

        public string BlindIndex(string? plaintext)
        {
            if (string.IsNullOrEmpty(plaintext))
                return string.Empty;

            var normalized = NormalizeContact(plaintext);
            using var hmac = new HMACSHA256(_hmacKey);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(normalized));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public string NormalizeContact(string? value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }

        public string NormalizeDigits(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var sb = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if (char.IsDigit(ch))
                    sb.Append(ch);
            }
            return sb.ToString();
        }

        public string BlindIndexDigits(string? value)
        {
            return BlindIndexRaw(NormalizeDigits(value));
        }

        private string BlindIndexRaw(string? normalized)
        {
            if (string.IsNullOrEmpty(normalized))
                return string.Empty;

            using var hmac = new HMACSHA256(_hmacKey);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(normalized));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static byte[] DeriveKey(string salt, string purpose, int length)
        {
            var material = string.IsNullOrEmpty(salt) ? "elist-default-dev-salt" : salt;
            using var derive = new Rfc2898DeriveBytes(
                Encoding.UTF8.GetBytes(material + "|" + purpose),
                Encoding.UTF8.GetBytes(purpose),
                100_000,
                HashAlgorithmName.SHA256);
            return derive.GetBytes(length);
        }
    }
}
