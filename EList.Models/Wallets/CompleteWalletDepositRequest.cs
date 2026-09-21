namespace EList.Models.Wallets
{
    /// <summary>Stub/debug: ручное подтверждение пополнения тарифного кошелька.</summary>
    public class CompleteWalletDepositRequest
    {
        public Guid? DepositId { get; set; }

        public string? ProviderPaymentId { get; set; }
    }
}
