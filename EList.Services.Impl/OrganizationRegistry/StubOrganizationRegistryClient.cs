using EList.Common.Configuration;
using EList.Models.Organizations;
using EList.Services.Interfaces;

namespace EList.Services.Impl.OrganizationRegistry
{
    /// <summary>
    /// Локальная проверка реквизитов (ИНН/ОГРН checksum) без внешнего API.
    /// Используется при registryMode=stub для локальной разработки.
    /// </summary>
    public class StubOrganizationRegistryClient : IOrganizationRegistryClient
    {
        private readonly bool _autoApproveInStub;

        public StubOrganizationRegistryClient()
        {
            _autoApproveInStub = !ConfigurationManager.AppSettings.Contains("organizationVerification:autoApproveInStub")
                || bool.Parse(ConfigurationManager.AppSettings["organizationVerification:autoApproveInStub"]);
        }

        public Task<OrganizationRegistryParty?> FindByInnAsync(string inn, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedInn = OrganizationRegistryValidation.NormalizeDigits(inn);
            if (string.IsNullOrWhiteSpace(normalizedInn) || !OrganizationRegistryValidation.IsValidInn(normalizedInn))
                return Task.FromResult<OrganizationRegistryParty?>(null);

            // Stub не ходит во внешний реестр — только подтверждает формат ИНН.
            return Task.FromResult<OrganizationRegistryParty?>(new OrganizationRegistryParty
            {
                Inn = normalizedInn,
                Status = "ACTIVE",
                LegalForm = normalizedInn.Length == 10
                    ? Models.Enums.OrganizationLegalForm.LegalEntity
                    : Models.Enums.OrganizationLegalForm.Ip,
                Name = $"STUB-{normalizedInn}"
            });
        }

        public Task<OrganizationRegistryCheckResult> CheckOrganizationAsync(
            OrganizationLegal legal,
            string organizationName,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (legal == null)
                return Task.FromResult(OrganizationRegistryCheckResult.Rejected("Юридические реквизиты отсутствуют"));

            var localValidation = OrganizationRegistryValidation.ValidateLocal(legal);
            if (localValidation != null)
                return Task.FromResult(OrganizationRegistryCheckResult.Rejected(localValidation));

            if (_autoApproveInStub)
                return Task.FromResult(OrganizationRegistryCheckResult.Verified(organizationName));

            return Task.FromResult(OrganizationRegistryCheckResult.Unavailable(
                "Stub-режим без autoApprove: заявка остаётся в pending"));
        }
    }
}
