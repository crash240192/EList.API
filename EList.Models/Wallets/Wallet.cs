namespace EList.Models.Wallets
{
    /// <summary>
    /// Кошелёк тарифного контура платформы (не билетные деньги).
    /// </summary>
    public class Wallet
    {
        public Guid Id { get; set; }

        public double Balance { get; set; }

        /// <summary>Устаревающее поле; при списании синхронизируется с LastChargeDate.</summary>
        public DateTimeOffset? PaidDate { get; set; }

        public Guid? TariffId { get; set; }

        /// <summary>Момент последнего успешного списания (= начало текущего оплаченного периода).</summary>
        public DateTimeOffset? LastChargeDate { get; set; }

        /// <summary>Когда пробовать следующее списание. null — тариф не активен / ждёт средств.</summary>
        public DateTimeOffset? NextChargeAt { get; set; }

        public Tariff Tariff { get; set; }
    }
}
