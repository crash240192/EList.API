namespace EList.Models.Wallets
{
    /// <summary>
    /// Кошелёк
    /// </summary>
    public class Wallet
    {
        /// <summary>
        /// Идентификатор кошелька
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Текущий баланс
        /// </summary>
        public double Balance { get; set; }

        /// <summary>
        /// Дата последней оплаты
        /// </summary>
        public DateTimeOffset? PaidDate { get; set; }

        /// <summary>
        /// Идентификатор тарифа
        /// </summary>
        public Guid? TariffId { get; set; }

        /// <summary>
        /// Дата последнего списания
        /// </summary>
        public DateTimeOffset? LastChargeDate { get; set; }


        /// <summary>
        /// Тариф
        /// </summary>
        public Tariff Tariff { get; set; }
    }
}
