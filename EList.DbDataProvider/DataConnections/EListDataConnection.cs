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
        public ITable<ContactOrganizationRelationDto> ContactOrganizationRelations => this.GetTable<ContactOrganizationRelationDto>();
        public ITable<SystemNotificationDto> SystemNotifications => this.GetTable<SystemNotificationDto>();
        public ITable<NotificationDto> UserNotifications => this.GetTable<NotificationDto>();
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
        public ITable<EventAlbumParametersDto> EventAlbumParameters => this.GetTable<EventAlbumParametersDto>();
        public ITable<AccountAlbumParametersDto> AccountAlbumParameters => this.GetTable<AccountAlbumParametersDto>();
        public ITable<AccountAlbumRelationDto> AccountAlbums => this.GetTable<AccountAlbumRelationDto>();
        public ITable<EventAlbumRelationDto> EventAlbums => this.GetTable<EventAlbumRelationDto>();
        public ITable<AccountAvatarDto> AccountAvatars => this.GetTable<AccountAvatarDto>();

        public ITable<OrganizationAvatarDto> OrganizationAvatars => this.GetTable<OrganizationAvatarDto>();
        public ITable<WalletDto> Wallets => this.GetTable<WalletDto>();
        public ITable<TariffDto> Tariffs => this.GetTable<TariffDto>();
        public ITable<TariffValidatorDto> TariffValidators => this.GetTable<TariffValidatorDto>();
        public ITable<OrganizationDto> Organizations => this.GetTable<OrganizationDto>();
        public ITable<OrganizationAccountRelationDto> OrganizationMembers => this.GetTable<OrganizationAccountRelationDto>();
        public ITable<OrganizationLegalDto> OrganizationLegal => this.GetTable<OrganizationLegalDto>();
        public ITable<OrganizationPayoutDto> OrganizationPayout => this.GetTable<OrganizationPayoutDto>();

        public ITable<OrderDto> Orders => this.GetTable<OrderDto>();
        public ITable<TicketDto> Tickets => this.GetTable<TicketDto>();
        public ITable<RefundDto> Refunds => this.GetTable<RefundDto>();
        public ITable<PaymentWebhookEventDto> PaymentWebhookEvents => this.GetTable<PaymentWebhookEventDto>();

        public ITable<ConversationDto> Conversations => this.GetTable<ConversationDto>();
        public ITable<MessageDto> Messages => this.GetTable<MessageDto>();

        public ITable<AnonymousAgeAgreementDto> AnonymousAgeAgreements => this.GetTable<AnonymousAgeAgreementDto>();
        public ITable<DocumentDto> Documents => this.GetTable<DocumentDto>();
        public ITable<AccountAgreementDto> Agreements => this.GetTable<AccountAgreementDto>();
        public ITable<OrganizationAgreementDto> OrganizationAgreements => this.GetTable<OrganizationAgreementDto>();
        public ITable<EventTemplateDto> EventTemplates => this.GetTable<EventTemplateDto>();
        public ITable<BugReportCategoryDto> BugReportCategories => this.GetTable<BugReportCategoryDto>();
        public ITable<BugReportDto> BugReports => this.GetTable<BugReportDto>();
        public ITable<BugReportFileDto> BugReportFiles => this.GetTable<BugReportFileDto>();

        public ITable<AccountPlatformRoleDto> AccountPlatformRoles => this.GetTable<AccountPlatformRoleDto>();
        public ITable<ReportReasonDto> ReportReasons => this.GetTable<ReportReasonDto>();
        public ITable<ContentReportDto> ContentReports => this.GetTable<ContentReportDto>();
        public ITable<ContentReportActionDto> ContentReportActions => this.GetTable<ContentReportActionDto>();
        public ITable<ModerationPenaltyDto> ModerationPenalties => this.GetTable<ModerationPenaltyDto>();
    }
}