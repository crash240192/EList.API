using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("organization_accounts_rls")]
    public class OrganizationAccountRelationDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("account_id")]
        public Guid AccountId { get; set; }

        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        [Column("role", DataType = DataType.Enum)]
        public OrganizationMemberRole Role { get; set; }

        [Column("active")]
        public bool Active { get; set; }

        [Column("invited_by")]
        public Guid? InvitedBy { get; set; }

        [Column("joined_at")]
        public DateTimeOffset JoinedAt { get; set; }


        [Association(ThisKey = nameof(AccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto Account { get; set; }

        [Association(ThisKey = nameof(OrganizationId), OtherKey = nameof(OrganizationDto.Id))]
        public OrganizationDto Organization { get; set; }

        [Association(ThisKey = nameof(InvitedBy), OtherKey = nameof(AccountDto.Id))]
        public AccountDto? InvitedByAccount { get; set; }
    }
}
