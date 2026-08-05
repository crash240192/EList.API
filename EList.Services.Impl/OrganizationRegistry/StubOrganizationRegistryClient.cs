using System.Text.RegularExpressions;
using EList.Models.Enums;
using EList.Models.Organizations;
using EList.Services.Interfaces;

namespace EList.Services.Impl.OrganizationRegistry
{
    /// <summary>
    /// Локальная проверка реквизитов (ИНН/ОГРН checksum) + заглушка реестра.
    /// При mode=stub подтверждает организации с валидными реквизитами.
    /// При mode=api выполняет локальную валидацию и при её успехе оставляет Unavailable,
    /// пока не будет подключён реальный провайдер ЕГРЮЛ/ЕГРИП.
    /// </summary>
    public class StubOrganizationRegistryClient : IOrganizationRegistryClient
    {
        private readonly string _mode;
        private readonly bool _autoApproveInStub;

        public StubOrganizationRegistryClient()
        {
            _mode = Common.Configuration.ConfigurationManager.AppSettings.Contains("organizationVerification:registryMode")
                ? Common.Configuration.ConfigurationManager.AppSettings["organizationVerification:registryMode"]
                : "stub";

            _autoApproveInStub = !Common.Configuration.ConfigurationManager.AppSettings.Contains("organizationVerification:autoApproveInStub")
                || bool.Parse(Common.Configuration.ConfigurationManager.AppSettings["organizationVerification:autoApproveInStub"]);
        }

        public Task<OrganizationRegistryCheckResult> CheckOrganizationAsync(
            OrganizationLegal legal,
            string organizationName,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (legal == null)
                return Task.FromResult(OrganizationRegistryCheckResult.Rejected("Юридические реквизиты отсутствуют"));

            var inn = NormalizeDigits(legal.Inn);
            if (string.IsNullOrWhiteSpace(inn))
                return Task.FromResult(OrganizationRegistryCheckResult.Rejected("ИНН не указан"));

            if (!IsValidInn(inn))
                return Task.FromResult(OrganizationRegistryCheckResult.Rejected("ИНН не прошёл проверку контрольной суммы"));

            var expectedInnLength = legal.LegalForm == OrganizationLegalForm.LegalEntity ? 10 : 12;
            if (inn.Length != expectedInnLength)
            {
                return Task.FromResult(OrganizationRegistryCheckResult.Rejected(
                    legal.LegalForm == OrganizationLegalForm.LegalEntity
                        ? "Для юрлица ИНН должен содержать 10 цифр"
                        : "Для ИП/самозанятого ИНН должен содержать 12 цифр"));
            }

            if (legal.LegalForm == OrganizationLegalForm.LegalEntity && string.IsNullOrWhiteSpace(legal.Kpp))
                return Task.FromResult(OrganizationRegistryCheckResult.Rejected("Для юрлица необходимо указать КПП"));

            var ogrn = NormalizeDigits(legal.Ogrn);
            if (!string.IsNullOrWhiteSpace(ogrn) && !IsValidOgrn(ogrn))
                return Task.FromResult(OrganizationRegistryCheckResult.Rejected("ОГРН/ОГРНИП не прошёл проверку контрольной суммы"));

            if (string.IsNullOrWhiteSpace(legal.HeadName))
                return Task.FromResult(OrganizationRegistryCheckResult.Rejected("Не указано ФИО руководителя"));

            // stub mode: auto-approve valid payload for local/dev
            if (string.Equals(_mode, "stub", StringComparison.OrdinalIgnoreCase))
            {
                if (_autoApproveInStub)
                    return Task.FromResult(OrganizationRegistryCheckResult.Verified(organizationName));

                return Task.FromResult(OrganizationRegistryCheckResult.Unavailable(
                    "Stub-режим без autoApprove: заявка остаётся в pending"));
            }

            // api mode placeholder — local validation passed, external registry not wired yet
            return Task.FromResult(OrganizationRegistryCheckResult.Unavailable(
                "Внешний клиент ЕГРЮЛ/ЕГРИП ещё не подключён; повторная попытка позже"));
        }

        private static string NormalizeDigits(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            return Regex.Replace(value, @"\D", string.Empty);
        }

        private static bool IsValidInn(string inn)
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

        private static bool IsValidOgrn(string ogrn)
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
