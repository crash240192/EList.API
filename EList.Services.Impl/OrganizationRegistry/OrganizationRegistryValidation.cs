using System.Text.RegularExpressions;
using EList.Models.Enums;
using EList.Models.Organizations;

namespace EList.Services.Impl.OrganizationRegistry
{
    /// <summary>
    /// Локальная валидация ИНН/ОГРН (checksum) без обращения к внешнему реестру.
    /// </summary>
    internal static class OrganizationRegistryValidation
    {
        public static string NormalizeDigits(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            return Regex.Replace(value, @"\D", string.Empty);
        }

        /// <summary>
        /// Возвращает текст ошибки или null, если локальная проверка пройдена.
        /// </summary>
        public static string? ValidateLocal(OrganizationLegal legal)
        {
            var inn = NormalizeDigits(legal.Inn);
            if (string.IsNullOrWhiteSpace(inn))
                return "ИНН не указан";

            if (!IsValidInn(inn))
                return "ИНН не прошёл проверку контрольной суммы";

            var expectedInnLength = legal.LegalForm == OrganizationLegalForm.LegalEntity ? 10 : 12;
            if (inn.Length != expectedInnLength)
            {
                return legal.LegalForm == OrganizationLegalForm.LegalEntity
                    ? "Для юрлица ИНН должен содержать 10 цифр"
                    : "Для ИП/самозанятого ИНН должен содержать 12 цифр";
            }

            if (legal.LegalForm == OrganizationLegalForm.LegalEntity && string.IsNullOrWhiteSpace(legal.Kpp))
                return "Для юрлица необходимо указать КПП";

            var ogrn = NormalizeDigits(legal.Ogrn);
            if (!string.IsNullOrWhiteSpace(ogrn) && !IsValidOgrn(ogrn))
                return "ОГРН/ОГРНИП не прошёл проверку контрольной суммы";

            if (string.IsNullOrWhiteSpace(legal.HeadName))
                return "Не указано ФИО руководителя";

            return null;
        }

        public static bool IsValidInn(string inn)
        {
            if (inn.Length == 10)
            {
                int[] coeffs = { 2, 4, 10, 3, 5, 9, 4, 6, 8 };
                var sum = 0;
                for (var i = 0; i < 9; i++)
                    sum += (inn[i] - '0') * coeffs[i];
                var check = sum % 11 % 10;
                return check == inn[9] - '0';
            }

            if (inn.Length == 12)
            {
                int[] coeffs11 = { 7, 2, 4, 10, 3, 5, 9, 4, 6, 8 };
                int[] coeffs12 = { 3, 7, 2, 4, 10, 3, 5, 9, 4, 6, 8 };

                var sum11 = 0;
                for (var i = 0; i < 10; i++)
                    sum11 += (inn[i] - '0') * coeffs11[i];
                var check11 = sum11 % 11 % 10;
                if (check11 != inn[10] - '0')
                    return false;

                var sum12 = 0;
                for (var i = 0; i < 11; i++)
                    sum12 += (inn[i] - '0') * coeffs12[i];
                var check12 = sum12 % 11 % 10;
                return check12 == inn[11] - '0';
            }

            return false;
        }

        public static bool IsValidOgrn(string ogrn)
        {
            if (ogrn.Length == 13)
            {
                if (!long.TryParse(ogrn.Substring(0, 12), out var num))
                    return false;
                var check = (int)(num % 11 % 10);
                return check == ogrn[12] - '0';
            }

            if (ogrn.Length == 15)
            {
                if (!long.TryParse(ogrn.Substring(0, 14), out var num))
                    return false;
                var check = (int)(num % 13 % 10);
                return check == ogrn[14] - '0';
            }

            return false;
        }
    }
}
