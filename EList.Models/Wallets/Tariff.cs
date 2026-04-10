namespace EList.Models.Wallets
{
    /// <summary>
    /// Тариф
    /// </summary>
    public class Tariff
    {
        /// <summary>
        /// Идентификатор тарифа
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Название тарифа
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Оплата за период
        /// </summary>
        public double Cost { get; set; }

        /// <summary>
        /// Период списания 
        /// </summary>
        public TimeSpan Period { get; set; }

        /// <summary>
        /// Идентификатор валидатора тарифа
        /// </summary>
        public Guid ValidatorId { get; set; }

        /// <summary>
        /// Валидатор тарифа
        /// </summary>
        public TariffValidator TariffValidator { get; set; }
    }
}
