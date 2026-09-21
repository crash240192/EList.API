namespace EList.Models.Wallets
{
    public class WalletTariffChargeResponse
    {
        public Guid Id { get; set; }
        public Guid WalletId { get; set; }
        public Guid TariffId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "RUB";
        public DateTimeOffset ChargedAt { get; set; }
        public DateTimeOffset NextChargeAt { get; set; }
        public double BalanceAfter { get; set; }
    }
}
