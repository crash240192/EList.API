using EList.DbDataProvider.Models;
using LinqToDB;
using LinqToDB.Data;

namespace EList.DbDataProvider.DataConnections
{
    // ReSharper disable once InconsistentNaming
    public class ElistDataConnection : DataConnection//, IPostgreSQLExtensions
    {
        //public ElistDataConnection() { }
        //public ElistDataConnection(string connectionName) : base(connectionName) { }

        //public ElistDataConnection(string connectionName) : base(connectionName)
        //{
        //    MappingSchema.SetConvertExpression<JToken, DataParameter>(p => new DataParameter(null, p == null ? null : JsonConvert.SerializeObject(p), DataType.BinaryJson));
        //    MappingSchema.SetConvertExpression<string, JToken>(p => JsonConvert.DeserializeObject<JToken>(p));
        //}

        public static void Configure(string[] connectionNames)
        {
            DefaultSettings = new ElistLinq2dbSettings(connectionNames);
        }

        public static void Configure()
        {
            DefaultSettings = new ElistLinq2dbSettings();
        }

        // с интерфейсами не работают специфические(сложные) типы данных (например: uuid[])
        public ITable<PersonInfoDto> Persons => this.GetTable<PersonInfoDto>();
        public ITable<ContactTypeDto> ContactTypes => this.GetTable<ContactTypeDto>();
        public ITable<ContactDataDto> ContactData => this.GetTable<ContactDataDto>();
        public ITable<AuthorizationDto> Authorization => this.GetTable<AuthorizationDto>();
        public ITable<AccountDto> Accounts => this.GetTable<AccountDto>();
        public ITable<ContactAccountRelationDto> ContactAccountRelations => this.GetTable<ContactAccountRelationDto>();
        public ITable<SystemNotificationDto> Notifications => this.GetTable<SystemNotificationDto>();
        public ITable<SubscriptionDto> Subscriptions => this.GetTable<SubscriptionDto>();
        public ITable<EventCategoryDto> EventCategories => this.GetTable<EventCategoryDto>();
        public ITable<EventTypeDto> EventTypes => this.GetTable<EventTypeDto>();
        public ITable<EventParametersDto> EventParameters => this.GetTable<EventParametersDto>();
        public ITable<EventTypeRelationDto> EventTypeRelations => this.GetTable<EventTypeRelationDto>();
        public ITable<EventDto> Events => this.GetTable<EventDto>();
        public ITable<EventOrganizatorDto> Organizators => this.GetTable<EventOrganizatorDto>();
        
        public ITable<ParticipationDto> Participations => this.GetTable<ParticipationDto>();
        public ITable<ParticipantsBlackListItemDto> BlackList => this.GetTable<ParticipantsBlackListItemDto>();
        public ITable<ParticipantsWhiteListItemDto> WhiteList => this.GetTable<ParticipantsWhiteListItemDto>();

        public ITable<EventsRatingDto> EventsRating => this.GetTable<EventsRatingDto>();
        public ITable<InvitationDto> Invitations => this.GetTable<InvitationDto>();
        
        public ITable<MediaAlbumDto> Albums => this.GetTable<MediaAlbumDto>();
        public ITable<FileAlbumRelationDto> AlbumFiles => this.GetTable<FileAlbumRelationDto>();

        public ITable<AccountAlbumRelationDto> AccountAlbums => this.GetTable<AccountAlbumRelationDto>();
        public ITable<EventAlbumRelationDto> EventAlbums => this.GetTable<EventAlbumRelationDto>();
        public ITable<AccountAvatarDto> AccountAvatars => this.GetTable<AccountAvatarDto>();

        public ITable<OrganizationAvatarDto> OrganizationAvatars => this.GetTable<OrganizationAvatarDto>();
        public ITable<WalletDto> Wallets => this.GetTable<WalletDto>();
        public ITable<TariffDto> Tariffs => this.GetTable<TariffDto>();
        public ITable<TariffValidatorDto> TariffValidators => this.GetTable<TariffValidatorDto>();
        public ITable<OrganizationDto> Organizations => this.GetTable<OrganizationDto>();
    }
}