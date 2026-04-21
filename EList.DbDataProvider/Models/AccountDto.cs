using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("accounts")]
    public class AccountDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("active")]
        public bool Active { get; set; }

        [Column("latitude")]
        public double? Latitude { get; set; }

        [Column("longitude")]
        public double? Longitude { get; set; }

        [Column("login")]
        public string Login { get; set; }

        [Column("password_hash")]
        public string PasswordHash { get; set; }

        [Column("registration_date")]
        public DateTimeOffset RegistrationDate { get; set; }

        [Column("last_seen_date")]
        public DateTimeOffset LastSeenDate { get; set; }

        [Column("last_action_date")]
        public DateTimeOffset LastActionDate { get; set; }

        [Column("wallet_id")]
        public Guid? WalletId { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(ContactAccountRelationDto.AccountId))]
        public List<ContactAccountRelationDto> ContactRelation { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(AuthorizationDto.AccountId))]
        public List<AuthorizationDto> AuthorizationTokens { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(PersonInfoDto.AccountId))]
        public PersonInfoDto PersonInfo { get; set; }
    }
}
