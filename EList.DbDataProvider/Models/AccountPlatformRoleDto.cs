using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("account_platform_roles")]
    public class AccountPlatformRoleDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("account_id")]
        public Guid AccountId { get; set; }

        [Column("role", DataType = DataType.Enum)]
        public PlatformRole Role { get; set; }

        [Column("active")]
        public bool Active { get; set; } = true;

        [Column("assigned_at")]
        public DateTimeOffset AssignedAt { get; set; }

        [Column("assigned_by")]
        public Guid? AssignedBy { get; set; }


        [Association(ThisKey = nameof(AccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto? Account { get; set; }

        [Association(ThisKey = nameof(AssignedBy), OtherKey = nameof(AccountDto.Id))]
        public AccountDto? AssignedByAccount { get; set; }
    }
}
