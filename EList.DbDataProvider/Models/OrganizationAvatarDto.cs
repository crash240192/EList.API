using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("organization_avatars_history")]
    public class OrganizationAvatarDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        [Column("photo_id")]
        public Guid PhotoId { get; set; }

        [Column("assignment_date")]
        public DateTimeOffset AssignmentDate { get; set; }
    }
}
