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

        /// <summary>
        /// Список подписавшихся на этого пользователя
        /// </summary>
        [Association(ThisKey = nameof(Id), OtherKey = nameof(SubscriptionDto.SubscribedToId))]
        public List<SubscriptionDto> Subscribers { get; set; }

        /// <summary>
        /// Список подписок этого пользователя
        /// </summary>
        [Association(ThisKey = nameof(Id), OtherKey = nameof(SubscriptionDto.SubscriberId))]
        public List<SubscriptionDto> Subscriptions { get; set; }

        /// <summary>
        /// Список альбомов пользователя
        /// </summary>
        [Association(ThisKey = nameof(Id), OtherKey = nameof(AccountAlbumRelationDto.AccountId))]
        public List<AccountAlbumRelationDto> AlbumRelations { get; set; }

        /// <summary>
        /// Список аватарок пользователя
        /// </summary>
        [Association(ThisKey = nameof(Id), OtherKey = nameof(AccountAvatarDto.AccountId))]
        public List<AccountAvatarDto> Avatars { get; set; }

        public Guid? AvatarId
        {
            get
            {
                return Avatars?.OrderByDescending(i => i.AssignmentDate)?.FirstOrDefault()?.PhotoId;
            }
        }
    }
}
