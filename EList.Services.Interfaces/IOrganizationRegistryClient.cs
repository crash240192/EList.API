using EList.Models.Enums;
using EList.Models.Organizations;

namespace EList.Services.Interfaces
{
    /// <summary>
    /// Клиент проверки организации в государственном реестре (ЕГРЮЛ/ЕГРИП).
    /// </summary>
    public interface IOrganizationRegistryClient
    {
        Task<OrganizationRegistryCheckResult> CheckOrganizationAsync(
            OrganizationLegal legal,
            string organizationName,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Результат проверки реквизитов в реестре.
    /// </summary>
    public class OrganizationRegistryCheckResult
    {
        public OrganizationRegistryCheckOutcome Outcome { get; set; }

        /// <summary>
        /// Официальное наименование из реестра (если получено).
        /// </summary>
        public string? OfficialName { get; set; }

        /// <summary>
        /// Причина отказа / описание ошибки.
        /// </summary>
        public string? Message { get; set; }

        public static OrganizationRegistryCheckResult Verified(string? officialName = null) => new()
        {
            Outcome = OrganizationRegistryCheckOutcome.Verified,
            OfficialName = officialName
        };

        public static OrganizationRegistryCheckResult Rejected(string message) => new()
        {
            Outcome = OrganizationRegistryCheckOutcome.Rejected,
            Message = message
        };

        public static OrganizationRegistryCheckResult Unavailable(string message) => new()
        {
            Outcome = OrganizationRegistryCheckOutcome.Unavailable,
            Message = message
        };
    }

    public enum OrganizationRegistryCheckOutcome
    {
        /// <summary>
        /// Реквизиты подтверждены, организация активна.
        /// </summary>
        Verified = 0,

        /// <summary>
        /// Реквизиты некорректны или организация ликвидирована/не найдена.
        /// </summary>
        Rejected = 1,

        /// <summary>
        /// Внешний сервис недоступен — оставить заявку в Pending и повторить позже.
        /// </summary>
        Unavailable = 2
    }
}
