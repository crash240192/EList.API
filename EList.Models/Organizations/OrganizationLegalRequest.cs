using EList.Models.Enums;

namespace EList.Models.Organizations
{
    /// <summary>
    /// Запрос на сохранение юридических реквизитов организации
    /// </summary>
    public class OrganizationLegalRequest
    {
        /// <summary>
        /// Юридическая форма
        /// </summary>
        public OrganizationLegalForm LegalForm { get; set; }

        /// <summary>
        /// ИНН
        /// </summary>
        public string? Inn { get; set; }

        /// <summary>
        /// ОГРН / ОГРНИП
        /// </summary>
        public string? Ogrn { get; set; }

        /// <summary>
        /// КПП
        /// </summary>
        public string? Kpp { get; set; }

        /// <summary>
        /// Юридический адрес
        /// </summary>
        public string? LegalAddress { get; set; }

        /// <summary>
        /// ФИО руководителя
        /// </summary>
        public string? HeadName { get; set; }

        /// <summary>
        /// Основание полномочий руководителя
        /// </summary>
        public string? HeadBasis { get; set; }
    }
}
