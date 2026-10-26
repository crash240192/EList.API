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

        /// <summary>
        /// Legacy WalletAutoRenew: следующее списание.
        /// PeriodPurchase: конец оплаченного периода (= <see cref="ActiveUntil"/>).
        /// </summary>
        public DateTimeOffset? NextChargeAt { get; set; }

        /// <summary>Конец активного платного периода (PeriodPurchase). Синоним NextChargeAt.</summary>
        public DateTimeOffset? ActiveUntil
        {
            get => NextChargeAt;
            set => NextChargeAt = value;
        }

        public Tariff Tariff { get; set; }

        /// <summary>
        /// Тариф, чьи лимиты сейчас применяются.
        /// PeriodPurchase: только активный entitlement или free — без «выбран, но не оплачен».
        /// </summary>
        public Guid? EffectiveTariffId { get; set; }

        /// <summary>Выбранный/текущий тариф сейчас даёт возможности (оплаченный период или бесплатный).</summary>
        public bool IsSelectedTariffActive { get; set; }

        /// <summary>Человекочитаемый статус биллинга для UI.</summary>
        public string? TariffBillingStatus { get; set; }

        /// <summary>Режим биллинга платформы: PeriodPurchase | WalletAutoRenew.</summary>
        public string? BillingMode { get; set; }
    }
}
