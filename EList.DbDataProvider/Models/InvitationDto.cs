using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("invitations")]
    public class InvitationDto
    {
        [Column("id"), Identity, PrimaryKey]
        public Guid Id { get; set; }

        [Column("inviter_id")]
        public Guid InviterAccountId { get; set; }

        [Column("invited_id")]
        public Guid InvitedAccountId { get; set; }

        [Column("inviter_org_id")]
        public Guid? InviterOrganizationId { get; set; }

        [Column("event_id")]
        public Guid EventId { get; set; }

        [Column("viewed")]
        public bool Viewed { get; set; } = false;

        [Column("creation_date")]
        public DateTimeOffset CreationDate { get; set; }

        [Association(ThisKey = nameof(InviterAccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto Inviter { get; set; }

        [Association(ThisKey = nameof(InvitedAccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto Invited { get; set; }

        [Association(ThisKey = nameof(EventId), OtherKey = nameof(EventDto.Id))]
        public EventDto Event { get; set; }
    }
}
