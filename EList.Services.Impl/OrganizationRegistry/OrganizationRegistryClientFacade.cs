using EList.Common.Configuration;
using EList.Common.CorrelationId;
using EList.Models.Organizations;
using EList.Services.Interfaces;

namespace EList.Services.Impl.OrganizationRegistry
{
    /// <summary>
    /// Выбор реализации реестра по organizationVerification:registryMode.
    /// stub — локальная проверка; dadata|api — DaData Suggestions API.
    /// </summary>
    public class OrganizationRegistryClientFacade : IOrganizationRegistryClient
    {
        private readonly IOrganizationRegistryClient _inner;

        public OrganizationRegistryClientFacade(ICorrelationIdProvider correlationIdProvider)
        {
            var mode = ConfigurationManager.AppSettings.Contains("organizationVerification:registryMode")
                ? ConfigurationManager.AppSettings["organizationVerification:registryMode"]
                : "stub";

            if (string.Equals(mode, "dadata", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mode, "api", StringComparison.OrdinalIgnoreCase))
            {
                _inner = new DaDataOrganizationRegistryClient(correlationIdProvider);
            }
            else
            {
                _inner = new StubOrganizationRegistryClient();
            }
        }

        public Task<OrganizationRegistryParty?> FindByInnAsync(string inn, CancellationToken cancellationToken = default)
            => _inner.FindByInnAsync(inn, cancellationToken);

        public Task<OrganizationRegistryCheckResult> CheckOrganizationAsync(
            OrganizationLegal legal,
            string organizationName,
            CancellationToken cancellationToken = default)
            => _inner.CheckOrganizationAsync(legal, organizationName, cancellationToken);
    }
}
