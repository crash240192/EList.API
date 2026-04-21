using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("accounts_avatars_history")]
    public class AccountAvatarDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("account_id")]
        public Guid AccountId { get; set; }

        [Column("photo_id")]
        public Guid PhotoId { get; set; }

        [Column("assignment_date")]
        public DateTimeOffset AssignmentDate { get; set; }
    }
}
