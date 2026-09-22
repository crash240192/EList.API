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

        /// <summary>Выбранный пользователем тариф (сохраняется даже если период не оплачен).</summary>
        public Guid? TariffId { get; set; }

        /// <summary>Момент последнего успешного списания (= начало текущего оплаченного периода).</summary>
        public DateTimeOffset? LastChargeDate { get; set; }

        /// <summary>Когда пробовать следующее списание. null — выбранный платный тариф не активен / ждёт средств.</summary>
        public DateTimeOffset? NextChargeAt { get; set; }

        public Tariff Tariff { get; set; }

        /// <summary>
        /// Тариф, чьи лимиты сейчас применяются: выбранный (если период активен или cost=0),
        /// иначе бесплатный тариф по умолчанию.
        /// </summary>
        public Guid? EffectiveTariffId { get; set; }

        /// <summary>Выбранный тариф сейчас даёт возможности (оплаченный период или бесплатный).</summary>
        public bool IsSelectedTariffActive { get; set; }

        /// <summary>Человекочитаемый статус биллинга для UI.</summary>
        public string? TariffBillingStatus { get; set; }
    }
}
