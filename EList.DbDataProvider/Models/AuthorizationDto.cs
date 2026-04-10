using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("public.authorization_token")]
    public class AuthorizationDto
    {
        [Column("token"), PrimaryKey, Identity]
        public Guid Token { get; set; }

        [Column("active")]
        public bool Active { get; set; }

        [Column("account_id")]
        public Guid AccountId { get; set; }

        [Column("client_hash")]
        public string ClientHash { get; set; }

        [Column("activation_key")]
        public string ActivationKey { get; set; }

        [Column("activation_attempts_remaining")]
        public int ActivationAttemptsRemaining { get; set; }

        [Column("creation_date")]
        public DateTimeOffset CreationDate { get; set; }

        [Column("authorization_date")]
        public DateTimeOffset AuthorizationDate { get; set; }

        [Association(ThisKey = nameof(AccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto Account { get; set; }
    }
}
