using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("wallet_tariff_charges")]
    public class WalletTariffChargeDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("wallet_id")]
        public Guid WalletId { get; set; }

        [Column("tariff_id")]
        public Guid TariffId { get; set; }

        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("currency")]
        public string Currency { get; set; }

        [Column("charged_at")]
        public DateTimeOffset ChargedAt { get; set; }

        [Column("next_charge_at")]
        public DateTimeOffset NextChargeAt { get; set; }

        [Column("balance_after")]
        public double BalanceAfter { get; set; }
    }
}
