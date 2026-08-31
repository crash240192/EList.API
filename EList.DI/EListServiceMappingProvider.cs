using EList.Common.CorrelationId;
using EList.Common.DI;
using EList.Common.Encryption;
using EList.Common.TemplateParser;
using EList.DbDataProvider.DataProviders;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Security;
using EList.FilestorageClient;
using EList.Repositories.Impl;
using EList.Repositories.Interfaces;
using EList.Services.Impl;
using EList.Services.Impl.AbuseProtection;
using EList.Services.Impl.OrganizationRegistry;
using EList.Services.Interfaces;
using EList.Sms;
using EList.Smtp;
using EList.Validators.Impl;
using EList.Validators.Interfaces;

namespace EList.DI
{
    public class EListServiceMappingProvider : IServiceMappingProvider
    {
        public ServiceMapping GetServiceMapping()
        {
            var mapper = new ServiceMapping();

            //DataProviders
            mapper.AddScoped<IAgreementDataProvider, AgreementDataProvider>();
            mapper.AddSingleton<ICorrelationIdProvider, Common.HttpRestClient.CorrelationIdProvider>();
            mapper.AddScoped<IPersonsDataProvider, PersonsDataProvider>();
            mapper.AddScoped<IContactsDataProvider, ContactsDataProvider>();
            mapper.AddScoped<IAuthorizationDataProvider, AuthorizationDataProvider>();
            mapper.AddScoped<IAccountsDataProvider, AccountsDataProvider>();
            mapper.AddScoped<INotificationsDataProvider, NotificationsDataProvider>();
            mapper.AddScoped<IDataConnectionProvider, DataConnectionProvider>();
            mapper.AddScoped<ISubscriptionsDataProvider, SubscriptionsDataProvider>();
            mapper.AddScoped<IEventsMetadataDataProvider, EventsMetadataDataProvider>();
            mapper.AddScoped<IEventsDataProvider, EventsDataProvider>();
            mapper.AddScoped<IEventOrganizatorsDataProvider, EventOrganizatorsDataProvider>();
            mapper.AddScoped<IParticipationsDataProvider, ParticipationsDataProvider>();
            mapper.AddScoped<IInvitationsDataProvider, InvitationsDataProvider>();
            mapper.AddScoped<IMediaDataProvider, MediaDataProvider>();
            mapper.AddScoped<IWalletsDataProvider, WalletsDataProvider>();
            mapper.AddScoped<IEventsRatingDataProvider, EventsRatingDataProvider>();
            mapper.AddScoped<IParticipantsBWListDataProvider, ParticipantsBWListDataProvider>();
            mapper.AddScoped<IConversationsDataProvider, ConversationsDataProvider>();
            mapper.AddScoped<IOrganizationsDataProvider, OrganizationsDataProvider>();
            mapper.AddScoped<IOrdersDataProvider, OrdersDataProvider>();
            mapper.AddScoped<IEventTemplatesDataProvider, EventTemplatesDataProvider>();
            mapper.AddScoped<IBugReportsDataProvider, BugReportsDataProvider>();
            mapper.AddScoped<IAccountPlatformRolesDataProvider, AccountPlatformRolesDataProvider>();
            mapper.AddScoped<IContentReportsDataProvider, ContentReportsDataProvider>();
            mapper.AddScoped<IModerationPenaltiesDataProvider, ModerationPenaltiesDataProvider>();

            //Services
            mapper.AddScoped<IPersonsService, PersonService>();
            mapper.AddScoped<IContactsService, ContactDataService>();
            mapper.AddScoped<IAuthorizationService, AuthorizationService>();
            mapper.AddScoped<IAccountsService, AccountsService>();
            mapper.AddScoped<ISystemNotificationsService, SystemNotificationsService>();
            mapper.AddScoped<ISubscriptionsService, SubscriptionsService>();
            mapper.AddScoped<IEventsService, EventsService>();
            mapper.AddScoped<IParticipationsService, ParticipationsService>();
            mapper.AddScoped<IInvitationsService, InvitationsService>();
            mapper.AddScoped<IMediaService, MediaService>();
            mapper.AddScoped<IWalletsService, WalletsService>();
            mapper.AddScoped<IEventOrganizatorsService, EventOrganizatorsService>();
            mapper.AddScoped<IEventsRatingService, EventsRatingService>();
            mapper.AddScoped<IConversationService, ConversationService>();
            mapper.AddScoped<INotificationsService, NotificationsService>();
            mapper.AddScoped<IAgreementService, AgreementService>();
            mapper.AddScoped<IOrganizationsService, OrganizationsService>();
            mapper.AddScoped<IEventTemplatesService, EventTemplatesService>();
            mapper.AddScoped<IBugReportsService, BugReportsService>();
            mapper.AddScoped<IAccountPlatformRolesService, AccountPlatformRolesService>();
            mapper.AddScoped<IContentReportsService, ContentReportsService>();
            mapper.AddScoped<IModerationPenaltiesService, ModerationPenaltiesService>();

            mapper.AddSingleton<WebSocketConnectionManager>();

            //Repositories
            mapper.AddScoped<IAgreementRepository, AgreementRepository>();
            mapper.AddScoped<IPersonsRepository, PersonsRepository>();
            mapper.AddScoped<IContactsRepository, ContactsRepository>();
            mapper.AddScoped<IAuthorizationRepository, AuthorizationRepository>();
            mapper.AddScoped<IAccountsRepository, AccountsRepository>();
            mapper.AddScoped<INotificationsRepository, NotificationsRepository>();
            mapper.AddScoped<ISubscriptionsRepository, SubscriptionsRepository>();
            mapper.AddScoped<IEventsMetadataRepository, EventsMetadataRepository>();
            mapper.AddScoped<IEventOrganizatorsRepository, EventOrganizatorsRepository>();
            mapper.AddScoped<IEventsRepository, EventsRepository>();
            mapper.AddScoped<IParticipationsRepository, ParticipationsRepository>();
            mapper.AddScoped<IInvitationsRepository, InvitationsRepository>();
            mapper.AddScoped<IMediaRepository, MediaRepository>();
            mapper.AddScoped<IWalletsRepository, WalletsRepository>();
            mapper.AddScoped<IEventsRatingRepository, EventsRatingRepository>();
            mapper.AddScoped<IParticipantsBWListRepository, ParticipantsBWListRepository>();
            mapper.AddScoped<IConversationRepository, ConversationRepository>();
            mapper.AddScoped<IOrganizationsRepository, OrganizationsRepository>();
            mapper.AddScoped<IOrdersRepository, OrdersRepository>();
            mapper.AddScoped<IEventTemplatesRepository, EventTemplatesRepository>();
            mapper.AddScoped<IBugReportsRepository, BugReportsRepository>();
            mapper.AddScoped<IAccountPlatformRolesRepository, AccountPlatformRolesRepository>();
            mapper.AddScoped<IContentReportsRepository, ContentReportsRepository>();
            mapper.AddScoped<IModerationPenaltiesRepository, ModerationPenaltiesRepository>();

            //Validators
            mapper.AddScoped<IPersonValidator, PersonValidator>();
            mapper.AddScoped<IPersonAccessValidator, PersonAccessValidator>();
            mapper.AddScoped<IUserDataValidator, UserDataValidator>();
            mapper.AddScoped<IContactValidator, ContactValidator>();
            mapper.AddScoped<IContactAccessValidator, ContactAccessValidator>();
            mapper.AddScoped<IEventAccessValidator, EventAccessValidator>();
            mapper.AddScoped<IAlbumAccessValidator, AlbumAccessValidator>();
            mapper.AddScoped<IParticipationAccessValidator, ParticipationAccessValidator>();
            mapper.AddScoped<ISubscriptionAccessValidator, SubscriptionAccessValidator>();
            mapper.AddScoped<IInvitationAccessValidator, InvitationAccessValidator>();
            mapper.AddScoped<IEventValidator, EventValidator>();
            mapper.AddScoped<IMediaAlbumValidator, MediaAlbumValidator>();
            mapper.AddScoped<IInvitationDataValidator, InvitationDataValidator>();
            mapper.AddScoped<IPagingValidator, PagingValidator>();

            //clients
            mapper.AddScoped<ISmtpClient, SmtpClientMailKit>();
            mapper.AddScoped<ISmsClient, GREENSMSSmsClient>();
            mapper.AddScoped<IFilestorageClient, FilestorageClient.FilestorageClient>();

            //support
            mapper.AddSingleton<ITemplateParser, TemplateParser>();
            mapper.AddSingleton<IEncryptionTool, EncryptionTool>();
            mapper.AddSingleton<IFieldEncryptor, FieldEncryptor>();
            mapper.AddSingleton<AbuseProtectionOptions>();
            mapper.AddSingleton<IEventCreateRateLimiter, EventCreateRateLimiter>();
            mapper.AddScoped<IAccountDataHolder, AccountDataHolder>();
            mapper.AddScoped<IOrganizationRegistryClient, OrganizationRegistryClientFacade>();

            return mapper;
        }
    }
}