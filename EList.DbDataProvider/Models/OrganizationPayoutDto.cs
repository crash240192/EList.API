using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("organization_payout")]
    public class OrganizationPayoutDto
    {
        [Column("organization_id"), PrimaryKey]
        public Guid OrganizationId { get; set; }

        [Column("bank_account")]
        public string? BankAccount { get; set; }

        [Column("bik")]
        public string? Bik { get; set; }

        [Column("bank_name")]
        public string? BankName { get; set; }

        [Column("tax_regime")]
        public string? TaxRegime { get; set; }

        [Column("provider", DataType = DataType.Enum)]
        public PaymentProvider? Provider { get; set; }

        [Column("provider_seller_id")]
        public string? ProviderSellerId { get; set; }

        [Column("onboarding_status", DataType = DataType.Enum)]
        public ProviderOnboardingStatus OnboardingStatus { get; set; }

        [Column("updated_by")]
        public Guid? UpdatedBy { get; set; }

        [Column("update_date")]
        public DateTimeOffset UpdateDate { get; set; }


        [Association(ThisKey = nameof(OrganizationId), OtherKey = nameof(OrganizationDto.Id))]
        public OrganizationDto Organization { get; set; }

        [Association(ThisKey = nameof(UpdatedBy), OtherKey = nameof(AccountDto.Id))]
        public AccountDto? UpdatedByAccount { get; set; }
    }
}
