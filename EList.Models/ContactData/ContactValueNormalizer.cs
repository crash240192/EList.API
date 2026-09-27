using System.Text;
using System.Text.RegularExpressions;

namespace EList.Models.ContactData
{
    /// <summary>
    /// Нормализация значений контактов под маски типов (телефон РФ и т.п.).
    /// </summary>
    public static class ContactValueNormalizer
    {
        private static readonly Regex DigitsOnly = new(@"\D", RegexOptions.Compiled);

        /// <summary>
        /// Если тип с телефонной маской — приводит +7XXXXXXXXXX / 8XXXXXXXXXX к +7 (XXX) XXX-XX-XX.
        /// </summary>
        public static string CanonicalizeForType(string value, ContactType contactType)
        {
            if (string.IsNullOrWhiteSpace(value) || contactType == null)
                return value;

            if (string.IsNullOrWhiteSpace(contactType.Mask) || !LooksLikePhoneMask(contactType.Mask))
                return value;

            return TryFormatRuPhoneMasked(value) ?? value;
        }

        public static bool LooksLikePhoneMask(string mask)
        {
            // Маска из БД: ^\+7\s\(\d{3}\)\s\d{3}-\d{2}-\d{2}$
            return mask.Contains("\\d", StringComparison.Ordinal)
                && (mask.Contains("+7", StringComparison.Ordinal) || mask.Contains("\\+7", StringComparison.Ordinal))
                && !mask.Contains('@');
        }

        /// <summary>
        /// 10 цифр номера РФ → +7 (XXX) XXX-XX-XX; иначе null.
        /// </summary>
        public static string? TryFormatRuPhoneMasked(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var digits = DigitsOnly.Replace(value, string.Empty);
            if (digits.Length >= 11 && (digits[0] == '7' || digits[0] == '8'))
                digits = digits.Substring(1, 10);
            else if (digits.Length > 10)
                digits = digits.Substring(0, 10);

            if (digits.Length != 10 || !digits.All(char.IsDigit))
                return null;

            var sb = new StringBuilder(18);
            sb.Append("+7 (");
            sb.Append(digits, 0, 3);
            sb.Append(") ");
            sb.Append(digits, 3, 3);
            sb.Append('-');
            sb.Append(digits, 6, 2);
            sb.Append('-');
            sb.Append(digits, 8, 2);
            return sb.ToString();
        }
    }
}
