namespace EList.Models.Wallets
{
    /// <summary>Покупка / продление / upgrade тарифа на период (режим PeriodPurchase).</summary>
    public class TariffPurchaseRequest
    {
        public Guid WalletId { get; set; }

        public Guid TariffId { get; set; }

        public string? ReturnUrl { get; set; }

        public string? IdempotencyKey { get; set; }
    }

    /// <summary>Предварительный расчёт стоимости без создания платежа.</summary>
    public class TariffPurchaseQuoteRequest
    {
        public Guid WalletId { get; set; }
        public Guid TariffId { get; set; }
    }

    public class TariffPurchaseQuoteResponse
    {
        public Guid WalletId { get; set; }
        public Guid TariffId { get; set; }
        public string TariffName { get; set; } = string.Empty;

        /// <summary>Purchase | Extend | Upgrade (или Free — без оплаты).</summary>
        public string Kind { get; set; } = string.Empty;

        public decimal ListPrice { get; set; }

        /// <summary>Скидка: остаток текущего периода + баланс (legacy credit).</summary>
        public decimal Credit { get; set; }

        /// <summary>К оплате сейчас (после credit). 0 → активация без эквайринга.</summary>
        public decimal AmountDue { get; set; }

        public int PeriodDays { get; set; }

        public DateTimeOffset? CurrentActiveUntil { get; set; }

        /// <summary>activeUntil после успешной оплаты / бесплатной активации.</summary>
        public DateTimeOffset NewActiveUntil { get; set; }

        public string? Message { get; set; }
    }

    public class CreateTariffPurchaseResponse
    {
        public WalletDepositResponse Deposit { get; set; } = null!;
        public string? ConfirmationUrl { get; set; }
        public string? ProviderPaymentId { get; set; }
        public bool PaidImmediately { get; set; }

        /// <summary>Кошелёк после активации (если PaidImmediately).</summary>
        public Wallet? Wallet { get; set; }
    }
}
