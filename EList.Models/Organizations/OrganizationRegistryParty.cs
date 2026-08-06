using EList.Models.Enums;

namespace EList.Models.Organizations
{
    /// <summary>
    /// Карточка организации/ИП из внешнего реестра (DaData / ЕГРЮЛ/ЕГРИП).
    /// </summary>
    public class OrganizationRegistryParty
    {
        public string? Inn { get; set; }

        public string? Ogrn { get; set; }

        public string? Kpp { get; set; }

        public string? Name { get; set; }

        public string? FullName { get; set; }

        public string? LegalAddress { get; set; }

        public string? HeadName { get; set; }

        public string? HeadPost { get; set; }

        public OrganizationLegalForm? LegalForm { get; set; }

        /// <summary>
        /// Статус в реестре: ACTIVE, LIQUIDATING, LIQUIDATED, BANKRUPT, REORGANIZING
        /// </summary>
        public string? Status { get; set; }

        public bool IsActive => string.Equals(Status, "ACTIVE", StringComparison.OrdinalIgnoreCase);
    }
}
