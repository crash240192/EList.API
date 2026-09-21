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
        public DateTimeOffset CreateDate { get; set; }
        public DateTimeOffset? PaidAt { get; set; }
    }
}
