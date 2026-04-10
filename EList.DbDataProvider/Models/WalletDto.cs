using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("public.wallets")]
    public class WalletDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }
        
        [Column("balance")]
        public double Balance { get; set; }

        [Column("paid_date")]
        public DateTimeOffset PaidDate { get; set; }

        [Column("tariff_id")]
        public Guid? TariffId { get; set; }

        [Column("last_charge_date")]
        public DateTimeOffset? LastChargeDate { get; set; }

        [Association(ThisKey = nameof(TariffId), OtherKey = nameof(TariffDto.Id))]
        public TariffDto Tariff { get; set; }
    }
}
