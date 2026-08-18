using AutoMapper;
using EList.DbDataProvider.Models;
using EList.Models.Accounts;
using EList.Models.Authorization;
using EList.Models.BugReports;
using EList.Models.ContentReports;
using EList.Models.ContactData;
using EList.Models.Conversations;
using EList.Models.EventOrganizators;
using EList.Models.EventTemplates;
using EList.Models.Events;
using EList.Models.Events.EventMetadata;
using EList.Models.EventsRating;
using EList.Models.Invitations;
using EList.Models.Media;
using EList.Models.Notifications;
using EList.Models.Orders;
using EList.Models.Organizations;
using EList.Models.Participation;
using EList.Models.Person;
using EList.Models.PlatformRoles;
using EList.Models.Subscriptions;
using EList.Models.UserAgreements;
using EList.Models.Wallets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EList.AutoMapperProfile
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            //if (!int.TryParse(ConfigurationManager.AppSettings["maxPageSize"], out var defaultPageSize))
            //{
            //    defaultPageSize = 100;
            //}

            CreateMap<AnonymousAgeAgreement, AnonymousAgeAgreementDto>().ReverseMap();
            CreateMap<AccountAgreement, AccountAgreementDto>().ReverseMap();

            CreateMap<AuthorizationDto, AuthorizationResponse>().ReverseMap();
            CreateMap<AuthorizationDto, Authorization>().ReverseMap();

            CreateMap<AccountDto, CreateAccountRequest>().ReverseMap();

            CreateMap<AccountDto, Account>();
            CreateMap<Account, AccountDto>()
                .ForMember(dest => dest.Avatars, opt => opt.Ignore());

            CreateMap<AccountAvatarDto, AccountAvatarItem>().ReverseMap();

            CreateMap<AccountDto, AccountPublicData>();

            CreateMap<PersonInfoDto, PersonInfo>().ReverseMap();

            CreateMap<ContactTypeDto, ContactType>().ReverseMap();
            CreateMap<ContactDataDto, ContactDataItem>().ReverseMap();

            CreateMap<Document, DocumentDto>().ReverseMap();

            CreateMap<SubscriptionDto, Subscription>().ReverseMap();
            CreateMap<DbDataProvider.Models.SearchRequests.SubscriptionsSearchRequest, SubscriptionsSearchRequest>().ReverseMap();

            CreateMap<SystemNotificationDto, SystemNotification>().ReverseMap();
            CreateMap<NotificationDto, Notification>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => MapNotificationTypeFromDb(src.Type)))
                .ForMember(dest => dest.Data, opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.Data) ? JsonConvert.DeserializeObject(src.Data) : null));
            CreateMap<Notification, NotificationDto>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type == null ? null : ((int)src.Type.Value).ToString()))
                .ForMember(dest => dest.Data, opt => opt.MapFrom(src => src.Data != null ? JObject.FromObject(src.Data).ToString() : null));

            CreateMap<EventCategoryDto, EventCategory>().ReverseMap();
            CreateMap<EventTypeDto, EventType>().ReverseMap();
            CreateMap<EventParametersDto, EventParameters>().ReverseMap();
            CreateMap<Event, EventDto>().ReverseMap().ForMember(dest => dest.Types, opt => opt.Ignore());
            CreateMap<EventDto, EventShort>().ReverseMap();
            CreateMap<EventsRatingDto, EventsRatingItem>().ReverseMap();
            CreateMap<EventOrganizatorDto, EventOrganizator>().ReverseMap();
            CreateMap<EventParametersRequest, SetEventParametersRequest>().ReverseMap();
            CreateMap<EventsSearchRequest, DbDataProvider.Models.SearchRequests.EventsSearchRequest>().ReverseMap();
            CreateMap<EventParticipantsSearchRequest, DbDataProvider.Models.SearchRequests.EventParticipantsSearchRequest>().ReverseMap();

            CreateMap<EventTemplateDto, EventTemplate>()
                .ForMember(dest => dest.TemplateBody, opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.TemplateBody) ? JsonConvert.DeserializeObject<CreateEventRequest>(src.TemplateBody) : null));
            CreateMap<EventTemplate, EventTemplateDto>()
                .ForMember(dest => dest.TemplateBody, opt => opt.MapFrom(src => src.TemplateBody != null ? JsonConvert.SerializeObject(src.TemplateBody) : null));
            CreateMap<EventTemplate, EventTemplateResponse>();

            CreateMap<ParticipantBlackListItem, ParticipantsBlackListItemDto>().ReverseMap();
            CreateMap<ParticipantWhiteListItem, ParticipantsWhiteListItemDto>().ReverseMap();

            CreateMap<Invitation, InvitationDto>().ReverseMap().ForMember(dest => dest.Inviter, opt => opt.Ignore());

            CreateMap<EventAlbumParameters, EventAlbumParametersDto>().ReverseMap();
            CreateMap<EventAlbumRequest, AlbumRequest>().ReverseMap();
            CreateMap<MediaAlbum, MediaAlbumDto>();
            CreateMap<MediaAlbumDto, MediaAlbum>()
                .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.EventRelation != null ? src.EventRelation.EventId : (Guid?)null));
            CreateMap<FileAlbumRelationDto, AlbumFile>().ReverseMap();

            CreateMap<TariffDto, Tariff>().ReverseMap();
            CreateMap<TariffValidatorDto, TariffValidator>().ReverseMap();
            CreateMap<WalletDto, Wallet>().ReverseMap();

            CreateMap<OrganizationDto, Organization>().ReverseMap();
            CreateMap<OrganizationRequest, OrganizationDto>();
            CreateMap<OrganizationAccountRelationDto, OrganizationMember>().ReverseMap();
            CreateMap<OrganizationLegalDto, OrganizationLegal>().ReverseMap();
            CreateMap<OrganizationLegalRequest, OrganizationLegal>();
            CreateMap<OrganizationPayoutDto, OrganizationPayout>().ReverseMap();
            CreateMap<OrganizationPayoutRequest, OrganizationPayout>();
            CreateMap<Organization, OrganizationResponse>();
            CreateMap<OrganizationMember, OrganizationMemberResponse>();
            CreateMap<OrganizationLegal, OrganizationLegalResponse>();
            CreateMap<OrganizationPayout, OrganizationPayoutResponse>();

            CreateMap<OrderDto, Order>().ReverseMap();
            CreateMap<TicketDto, Ticket>().ReverseMap();
            CreateMap<RefundDto, Refund>().ReverseMap();
            CreateMap<PaymentWebhookEventDto, PaymentWebhookEvent>().ReverseMap();
            CreateMap<Order, OrderResponse>();
            CreateMap<Ticket, TicketResponse>();
            CreateMap<Refund, RefundResponse>();

            CreateMap<MessageDto, Message>().ReverseMap();
            CreateMap<MessageRequest, MessageDto>().ReverseMap();
            CreateMap<ConversationDto, Conversation>().ReverseMap();
            CreateMap<ConversationRequest, ConversationDto>().ReverseMap();

            CreateMap<BugReportCategoryDto, BugReportCategory>().ReverseMap();
            CreateMap<BugReportDto, BugReport>()
                .ForMember(dest => dest.FileIds, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Reporter, opt => opt.Ignore());
            CreateMap<BugReport, BugReportDto>()
                .ForMember(dest => dest.Files, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.ReporterAccount, opt => opt.Ignore());
            CreateMap<BugReport, BugReportResponse>();

            CreateMap<AccountPlatformRoleDto, AccountPlatformRole>()
                .ForMember(dest => dest.Account, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedByAccount, opt => opt.Ignore());
            CreateMap<AccountPlatformRole, AccountPlatformRoleDto>()
                .ForMember(dest => dest.Account, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedByAccount, opt => opt.Ignore());

            CreateMap<ReportReasonDto, ReportReason>().ReverseMap();
            CreateMap<ContentReportActionDto, ContentReportAction>()
                .ForMember(dest => dest.ActorAccount, opt => opt.Ignore());
            CreateMap<ContentReportAction, ContentReportActionDto>()
                .ForMember(dest => dest.ActorAccount, opt => opt.Ignore())
                .ForMember(dest => dest.Report, opt => opt.Ignore());

            CreateMap<ContentReportDto, ContentReport>()
                .ForMember(dest => dest.Reason, opt => opt.Ignore())
                .ForMember(dest => dest.Reporter, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedToAccount, opt => opt.Ignore())
                .ForMember(dest => dest.ResolvedByAccount, opt => opt.Ignore())
                .ForMember(dest => dest.Event, opt => opt.Ignore())
                .ForMember(dest => dest.Message, opt => opt.Ignore())
                .ForMember(dest => dest.Actions, opt => opt.Ignore());
            CreateMap<ContentReport, ContentReportDto>()
                .ForMember(dest => dest.Reason, opt => opt.Ignore())
                .ForMember(dest => dest.ReporterAccount, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedToAccount, opt => opt.Ignore())
                .ForMember(dest => dest.ResolvedByAccount, opt => opt.Ignore())
                .ForMember(dest => dest.Event, opt => opt.Ignore())
                .ForMember(dest => dest.Message, opt => opt.Ignore())
                .ForMember(dest => dest.Conversation, opt => opt.Ignore())
                .ForMember(dest => dest.ReportedAccount, opt => opt.Ignore())
                .ForMember(dest => dest.Organization, opt => opt.Ignore())
                .ForMember(dest => dest.EventOrganizator, opt => opt.Ignore())
                .ForMember(dest => dest.Album, opt => opt.Ignore())
                .ForMember(dest => dest.Actions, opt => opt.Ignore());
            CreateMap<ContentReport, ContentReportResponse>();
            CreateMap<ContentReportsSearchRequest, DbDataProvider.Models.SearchRequests.ContentReportsSearchRequest>().ReverseMap();

            CreateMap<Models.Enums.Gender, DbDataProvider.Models.Enums.Gender>().ReverseMap();
            CreateMap<Models.Enums.SystemNotificationType, DbDataProvider.Models.Enums.SystemNotificationType>().ReverseMap();
            CreateMap<Models.Enums.EventRatingType, DbDataProvider.Models.Enums.EventRatingType>().ReverseMap();
            CreateMap<Models.Enums.DocumentType, DbDataProvider.Models.Enums.DocumentType>().ReverseMap();
            CreateMap<Models.Enums.OrganizationMemberRole, DbDataProvider.Models.Enums.OrganizationMemberRole>().ReverseMap();
            CreateMap<Models.Enums.OrganizationVerificationStatus, DbDataProvider.Models.Enums.OrganizationVerificationStatus>().ReverseMap();
            CreateMap<Models.Enums.OrganizationLegalForm, DbDataProvider.Models.Enums.OrganizationLegalForm>().ReverseMap();
            CreateMap<Models.Enums.PaymentProvider, DbDataProvider.Models.Enums.PaymentProvider>().ReverseMap();
            CreateMap<Models.Enums.OrderStatus, DbDataProvider.Models.Enums.OrderStatus>().ReverseMap();
            CreateMap<Models.Enums.TicketStatus, DbDataProvider.Models.Enums.TicketStatus>().ReverseMap();
            CreateMap<Models.Enums.RefundStatus, DbDataProvider.Models.Enums.RefundStatus>().ReverseMap();
            CreateMap<Models.Enums.ProviderOnboardingStatus, DbDataProvider.Models.Enums.ProviderOnboardingStatus>().ReverseMap();
            CreateMap<Models.Enums.BugReportStatus, DbDataProvider.Models.Enums.BugReportStatus>().ReverseMap();
            CreateMap<Models.Enums.PlatformRole, DbDataProvider.Models.Enums.PlatformRole>().ReverseMap();
            CreateMap<Models.Enums.ReportTargetType, DbDataProvider.Models.Enums.ReportTargetType>().ReverseMap();
            CreateMap<Models.Enums.ReportTargetScope, DbDataProvider.Models.Enums.ReportTargetScope>().ReverseMap();
            CreateMap<Models.Enums.ReportSeverity, DbDataProvider.Models.Enums.ReportSeverity>().ReverseMap();
            CreateMap<Models.Enums.ReportQueue, DbDataProvider.Models.Enums.ReportQueue>().ReverseMap();
            CreateMap<Models.Enums.ReportStatus, DbDataProvider.Models.Enums.ReportStatus>().ReverseMap();
            CreateMap<Models.Enums.ReportResolutionAction, DbDataProvider.Models.Enums.ReportResolutionAction>().ReverseMap();
            CreateMap<Models.Enums.ReportActorContext, DbDataProvider.Models.Enums.ReportActorContext>().ReverseMap();
            CreateMap<Models.Enums.ModerationPenaltyType, DbDataProvider.Models.Enums.ModerationPenaltyType>().ReverseMap();
            CreateMap<ModerationPenaltyDto, ModerationPenalty>().ReverseMap();
        }

        private static UserNotificationType? MapNotificationTypeFromDb(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return null;

            if (int.TryParse(type, out var numericType) && Enum.IsDefined(typeof(UserNotificationType), numericType))
                return (UserNotificationType)numericType;

            if (Enum.TryParse<UserNotificationType>(type, ignoreCase: true, out var namedType))
                return namedType;

            return null;
        }
    }
}