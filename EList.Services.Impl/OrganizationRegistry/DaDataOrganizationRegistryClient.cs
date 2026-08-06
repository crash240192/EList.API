using System.Net.Http;
using System.Text.RegularExpressions;
using EList.Common.Configuration;
using EList.Common.CorrelationId;
using EList.Common.HttpRestClient;
using EList.Common.Logger;
using EList.Models.Enums;
using EList.Models.Organizations;
using EList.Services.Interfaces;
using Newtonsoft.Json;
using NLog;

namespace EList.Services.Impl.OrganizationRegistry
{
    /// <summary>
    /// Клиент DaData Suggestions API (ЕГРЮЛ/ЕГРИП): поиск по ИНН и сверка реквизитов.
    /// </summary>
    public class DaDataOrganizationRegistryClient : IOrganizationRegistryClient
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.OrganizationRegistry.DaDataOrganizationRegistryClient.";
        #endregion

        private const string DefaultBaseUrl = "https://suggestions.dadata.ru/suggestions/api/4_1/rs";

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly string? _secretKey;
        private readonly TimeSpan _timeout;

        public DaDataOrganizationRegistryClient(ICorrelationIdProvider correlationIdProvider)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));

            _baseUrl = ConfigurationManager.AppSettings.Contains("organizationVerification:dadata:baseUrl")
                ? ConfigurationManager.AppSettings["organizationVerification:dadata:baseUrl"]
                : DefaultBaseUrl;

            _apiKey = ConfigurationManager.AppSettings.Contains("organizationVerification:dadata:apiKey")
                ? ConfigurationManager.AppSettings["organizationVerification:dadata:apiKey"]
                : string.Empty;

            _secretKey = ConfigurationManager.AppSettings.Contains("organizationVerification:dadata:secretKey")
                ? ConfigurationManager.AppSettings["organizationVerification:dadata:secretKey"]
                : null;

            _timeout = ConfigurationManager.AppSettings.Contains("organizationVerification:dadata:timeout")
                ? TimeSpan.Parse(ConfigurationManager.AppSettings["organizationVerification:dadata:timeout"])
                : TimeSpan.FromSeconds(15);
        }

        public async Task<OrganizationRegistryParty?> FindByInnAsync(string inn, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedInn = OrganizationRegistryValidation.NormalizeDigits(inn);
            if (string.IsNullOrWhiteSpace(normalizedInn))
                return null;

            if (!OrganizationRegistryValidation.IsValidInn(normalizedInn))
                return null;

            var suggestion = await FindPartySuggestionAsync(normalizedInn, cancellationToken);
            return suggestion == null ? null : MapParty(suggestion);
        }

        public async Task<OrganizationRegistryCheckResult> CheckOrganizationAsync(
            OrganizationLegal legal,
            string organizationName,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (legal == null)
                return OrganizationRegistryCheckResult.Rejected("Юридические реквизиты отсутствуют");

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                return OrganizationRegistryCheckResult.Unavailable(
                    "DaData apiKey не задан в конфигурации organizationVerification:dadata:apiKey");
            }

            var localValidation = OrganizationRegistryValidation.ValidateLocal(legal);
            if (localValidation != null)
                return OrganizationRegistryCheckResult.Rejected(localValidation);

            var inn = OrganizationRegistryValidation.NormalizeDigits(legal.Inn);
            DaDataPartySuggestion? suggestion;
            try
            {
                suggestion = await FindPartySuggestionAsync(inn, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                var correlationId = _correlationIdProvider.Get();
                logger.Warn(correlationId, null, $"{LOGGER_NAME}{nameof(CheckOrganizationAsync)}",
                    $"DaData unavailable: {ex.Message}", null);
                return OrganizationRegistryCheckResult.Unavailable($"Сервис DaData временно недоступен: {ex.Message}");
            }

            if (suggestion?.Data == null)
                return OrganizationRegistryCheckResult.Rejected("Организация/ИП не найдена в реестре по указанному ИНН");

            var data = suggestion.Data;
            var officialName = data.Name?.ShortWithOpf
                ?? data.Name?.FullWithOpf
                ?? suggestion.Value
                ?? organizationName;

            var status = data.State?.Status;
            if (!string.Equals(status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                return OrganizationRegistryCheckResult.Rejected(
                    string.IsNullOrWhiteSpace(status)
                        ? "Статус организации в реестре неизвестен"
                        : $"Организация имеет статус '{status}' (требуется ACTIVE)");
            }

            var expectedType = legal.LegalForm == OrganizationLegalForm.LegalEntity ? "LEGAL" : "INDIVIDUAL";
            if (!string.IsNullOrWhiteSpace(data.Type)
                && !string.Equals(data.Type, expectedType, StringComparison.OrdinalIgnoreCase))
            {
                return OrganizationRegistryCheckResult.Rejected(
                    legal.LegalForm == OrganizationLegalForm.LegalEntity
                        ? "По ИНН в реестре найдено ИП, а указана форма «юридическое лицо»"
                        : "По ИНН в реестре найдено юрлицо, а указана форма ИП/самозанятый");
            }

            var registryOgrn = OrganizationRegistryValidation.NormalizeDigits(data.Ogrn);
            var requestOgrn = OrganizationRegistryValidation.NormalizeDigits(legal.Ogrn);
            if (!string.IsNullOrWhiteSpace(requestOgrn)
                && !string.IsNullOrWhiteSpace(registryOgrn)
                && !string.Equals(requestOgrn, registryOgrn, StringComparison.Ordinal))
            {
                return OrganizationRegistryCheckResult.Rejected("ОГРН/ОГРНИП не совпадает с данными реестра");
            }

            if (legal.LegalForm == OrganizationLegalForm.LegalEntity)
            {
                var registryKpp = OrganizationRegistryValidation.NormalizeDigits(data.Kpp);
                var requestKpp = OrganizationRegistryValidation.NormalizeDigits(legal.Kpp);
                if (!string.IsNullOrWhiteSpace(requestKpp)
                    && !string.IsNullOrWhiteSpace(registryKpp)
                    && !string.Equals(requestKpp, registryKpp, StringComparison.Ordinal))
                {
                    return OrganizationRegistryCheckResult.Rejected("КПП не совпадает с данными реестра");
                }
            }

            var registryHead = ResolveHeadName(data);
            if (!string.IsNullOrWhiteSpace(legal.HeadName)
                && !string.IsNullOrWhiteSpace(registryHead)
                && !NamesMatch(legal.HeadName, registryHead))
            {
                return OrganizationRegistryCheckResult.Rejected(
                    $"ФИО руководителя не совпадает с реестром (ожидалось: {registryHead})");
            }

            if (!string.IsNullOrWhiteSpace(organizationName)
                && !NameMatchesOrganization(organizationName, data, suggestion.Value))
            {
                return OrganizationRegistryCheckResult.Rejected(
                    $"Наименование организации не совпадает с реестром (ожидалось: {officialName})");
            }

            return OrganizationRegistryCheckResult.Verified(officialName);
        }

        private async Task<DaDataPartySuggestion?> FindPartySuggestionAsync(string query, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(_apiKey))
                return null;

            var correlationId = _correlationIdProvider.Get();
            var client = new HttpRestClient2(correlationId, _baseUrl, $"Token {_apiKey}");

            var headers = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(_secretKey))
                headers["X-Secret"] = _secretKey;

            var body = new
            {
                query,
                count = 1,
                branch_type = "MAIN"
            };

            var response = await client.PostAsync<DaDataPartyResponse>(
                "findById/party",
                headers.Count > 0 ? headers : null,
                body,
                _timeout);

            return response?.Suggestions?.FirstOrDefault(i => i?.Data != null);
        }

        private static OrganizationRegistryParty MapParty(DaDataPartySuggestion suggestion)
        {
            var data = suggestion.Data!;
            OrganizationLegalForm? legalForm = null;
            if (string.Equals(data.Type, "LEGAL", StringComparison.OrdinalIgnoreCase))
                legalForm = OrganizationLegalForm.LegalEntity;
            else if (string.Equals(data.Type, "INDIVIDUAL", StringComparison.OrdinalIgnoreCase))
                legalForm = OrganizationLegalForm.Ip;

            return new OrganizationRegistryParty
            {
                Inn = data.Inn,
                Ogrn = data.Ogrn,
                Kpp = data.Kpp,
                Name = data.Name?.ShortWithOpf ?? suggestion.Value,
                FullName = data.Name?.FullWithOpf,
                LegalAddress = data.Address?.Data?.Source
                    ?? data.Address?.UnrestrictedValue
                    ?? data.Address?.Value,
                HeadName = ResolveHeadName(data),
                HeadPost = data.Management?.Post,
                LegalForm = legalForm,
                Status = data.State?.Status
            };
        }

        private static string? ResolveHeadName(DaDataPartyData data)
        {
            if (!string.IsNullOrWhiteSpace(data.Management?.Name))
                return data.Management.Name.Trim();

            if (data.Fio == null)
                return null;

            var parts = new[] { data.Fio.Surname, data.Fio.Name, data.Fio.Patronymic }
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Select(i => i!.Trim());
            var fio = string.Join(" ", parts);
            return string.IsNullOrWhiteSpace(fio) ? null : fio;
        }

        private static bool NameMatchesOrganization(string organizationName, DaDataPartyData data, string? suggestionValue)
        {
            var candidates = new[]
            {
                data.Name?.ShortWithOpf,
                data.Name?.FullWithOpf,
                data.Name?.Short,
                data.Name?.Full,
                suggestionValue
            };

            return candidates.Any(candidate => !string.IsNullOrWhiteSpace(candidate) && NamesMatch(organizationName, candidate!));
        }

        private static bool NamesMatch(string left, string right)
        {
            var a = NormalizeName(left);
            var b = NormalizeName(right);
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
                return false;

            if (string.Equals(a, b, StringComparison.Ordinal))
                return true;

            return a.Contains(b, StringComparison.Ordinal) || b.Contains(a, StringComparison.Ordinal);
        }

        private static string NormalizeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var normalized = value.Trim().ToUpperInvariant();
            normalized = normalized
                .Replace("Ё", "Е", StringComparison.Ordinal)
                .Replace("\"", string.Empty, StringComparison.Ordinal)
                .Replace("«", string.Empty, StringComparison.Ordinal)
                .Replace("»", string.Empty, StringComparison.Ordinal)
                .Replace("'", string.Empty, StringComparison.Ordinal);

            normalized = Regex.Replace(normalized, @"\s+", " ");
            return normalized;
        }
    }
}
