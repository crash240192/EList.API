namespace EList.Models.Organizations
{
    /// <summary>
    /// Запрос на сохранение банковских реквизитов организации
    /// </summary>
    public class OrganizationPayoutRequest
    {
        /// <summary>
        /// Расчётный счёт
        /// </summary>
        public string? BankAccount { get; set; }

        /// <summary>
        /// БИК
        /// </summary>
        public string? Bik { get; set; }

        /// <summary>
        /// Название банка
        /// </summary>
        public string? BankName { get; set; }

        /// <summary>
        /// Налоговый режим
        /// </summary>
        public string? TaxRegime { get; set; }
    }
}
