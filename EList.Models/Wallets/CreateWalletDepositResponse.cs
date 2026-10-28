using EList.Models.Enums;

namespace EList.Models.Wallets
{
    /// <summary>Ответ на создание пополнения тарифного кошелька.</summary>
    public class CreateWalletDepositResponse
    {
        public WalletDepositResponse Deposit { get; set; }

        public string? ConfirmationUrl { get; set; }

        public string? ProviderPaymentId { get; set; }

        /// <summary>true если stub/провайдер сразу подтвердил оплату.</summary>
        public bool PaidImmediately { get; set; }
    }

    public class WalletDepositResponse
    {
        public Guid Id { get; set; }
        public Guid WalletId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RUB";
        public WalletDepositStatus Status { get; set; }
        public PaymentProvider? Provider { get; set; }
        public string? ProviderPaymentId { get; set; }
        /// <summary>Номер заказа у банка / в чеке (напр. tariff:A7K9M2X3).</summary>
        public string? ProviderOrderId { get; set; }
        public DateTimeOffset CreateDate { get; set; }
        public DateTimeOffset? PaidAt { get; set; }
        /// <summary>Баланс сразу после зачисления (до возможного автосписания тарифа).</summary>
        public double? BalanceAfter { get; set; }

        public Guid? TariffId { get; set; }
        public WalletTariffPurchaseKind PurchaseKind { get; set; }
        public decimal CreditApplied { get; set; }
        public DateTimeOffset? PeriodStart { get; set; }
        public DateTimeOffset? PeriodEnd { get; set; }
    }
}
