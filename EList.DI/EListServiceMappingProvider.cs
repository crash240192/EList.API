using EList.Common.CorrelationId;
using EList.Common.DI;
using EList.Common.Encryption;
using EList.Common.TemplateParser;
using EList.DbDataProvider.DataProviders;
using EList.DbDataProvider.Interfaces;
using EList.FilestorageClient;
using EList.Repositories.Impl;
using EList.Repositories.Interfaces;
using EList.Services.Impl;
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

            //Services
            mapper.AddScoped<IPersonsService, PersonService>();
            mapper.AddScoped<IContactsService, ContactDataService>();
            mapper.AddScoped<IAuthorizationService, AuthorizationService>();
            mapper.AddScoped<IAccountsService, AccountsService>();
            mapper.AddScoped<INotificationsService, NotificationsService>();
            mapper.AddScoped<ISubscriptionsService, SubscriptionsService>();
            mapper.AddScoped<IEventsService, EventsService>();
            mapper.AddScoped<IParticipationsService, ParticipationsService>();
            mapper.AddScoped<IInvitationsService, InvitationsService>();
            mapper.AddScoped<IMediaService, MediaService>();
            mapper.AddScoped<IWalletsService, WalletsService>();

            //Repositories
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

            //Validators
            mapper.AddScoped<IPersonValidator, PersonValidator>();
            mapper.AddScoped<IUserDataValidator, UserDataValidator>();

            //clients
            mapper.AddScoped<ISmtpClient, SmtpClientMailKit>();
            mapper.AddScoped<ISmsClient, GREENSMSSmsClient>();
            mapper.AddScoped<IFilestorageClient, FilestorageClient.FilestorageClient>();

            //support
            mapper.AddSingleton<ITemplateParser, TemplateParser>();
            mapper.AddSingleton<IEncryptionTool, EncryptionTool>();
            mapper.AddScoped<IAccountDataHolder, AccountDataHolder>();
            mapper.AddScoped<IDebtCollectorUtility, DebtCollectorUtility>();

            return mapper;
        }
    }
}