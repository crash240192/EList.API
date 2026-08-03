using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("organizations")]
    public class OrganizationDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("active")]
        public bool Active { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("address")]
        public string? Address { get; set; }

        [Column("latitude")]
        public double? Latitude { get; set; }

        [Column("longitude")]
        public double? Longitude { get; set; }

        [Column("wallet_id")]
        public Guid? WalletId { get; set; }

        [Column("created_by_account_id")]
        public Guid? CreatedByAccountId { get; set; }

        [Column("verification_status", DataType = DataType.Enum)]
        public OrganizationVerificationStatus VerificationStatus { get; set; }

        [Column("verification_reject_reason")]
        public string? VerificationRejectReason { get; set; }

        [Column("can_sell_tickets")]
        public bool CanSellTickets { get; set; }

        [Column("create_date")]
        public DateTimeOffset CreateDate { get; set; }

        [Column("update_date")]
        public DateTimeOffset UpdateDate { get; set; }


        [Association(ThisKey = nameof(WalletId), OtherKey = nameof(WalletDto.Id))]
        public WalletDto? Wallet { get; set; }

        [Association(ThisKey = nameof(CreatedByAccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto? CreatedByAccount { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(OrganizationAccountRelationDto.OrganizationId))]
        public List<OrganizationAccountRelationDto> Members { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(OrganizationLegalDto.OrganizationId))]
        public OrganizationLegalDto? Legal { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(OrganizationPayoutDto.OrganizationId))]
        public OrganizationPayoutDto? Payout { get; set; }
    }
}
