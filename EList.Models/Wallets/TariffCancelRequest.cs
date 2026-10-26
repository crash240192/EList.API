namespace EList.Models.Wallets
{
    /// <summary>Предварительный расчёт отмены тарифа с возвратом за неиспользованные дни.</summary>
    public class TariffCancelQuoteRequest
    {
        public Guid WalletId { get; set; }
    }

    public class TariffCancelQuoteResponse
    {
        public Guid WalletId { get; set; }
        public Guid TariffId { get; set; }
        public string TariffName { get; set; } = string.Empty;
        public DateTimeOffset? ActiveUntil { get; set; }

        /// <summary>Остаток оплаченного периода (дни, дробные).</summary>
        public decimal RemainingDays { get; set; }

        /// <summary>Длительность купленного периода тарифа (дни).</summary>
        public int PeriodDays { get; set; }

        public Guid? DepositId { get; set; }

        /// <summary>Сколько было оплачено картой по текущему entitlement-депозиту.</summary>
        public decimal AmountPaid { get; set; }

        /// <summary>Уже возвращено / в процессе по этому депозиту.</summary>
        public decimal AlreadyRefunded { get; set; }

        /// <summary>Сумма к возврату на карту сейчас (пропорция неиспользованных дней).</summary>
        public decimal RefundAmount { get; set; }

        public string Currency { get; set; } = "RUB";

        public Guid? FallbackTariffId { get; set; }
        public string? FallbackTariffName { get; set; }

        public string? Message { get; set; }
    }

    /// <summary>Отмена активного платного тарифа + возврат за неиспользованные дни.</summary>
    public class TariffCancelRequest
    {
        public Guid WalletId { get; set; }
        public string? Reason { get; set; }
        public string? IdempotencyKey { get; set; }

        /// <summary>
        /// Опциональная проверка: клиент подтверждает сумму из quote.
        /// Если передана и расходится с пересчётом — ошибка.
        /// </summary>
        public decimal? ConfirmRefundAmount { get; set; }
    }

    public class CreateTariffCancelResponse
    {
        public WalletTariffRefundResponse Refund { get; set; } = null!;
        public decimal RefundAmount { get; set; }
        public bool PaidImmediately { get; set; }
        public Wallet? Wallet { get; set; }
        public string? Message { get; set; }
    }
}
