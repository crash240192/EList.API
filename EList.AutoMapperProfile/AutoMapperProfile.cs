using AutoMapper;
using EList.DbDataProvider.Models;
using EList.Models.Accounts;
using EList.Models.Authorization;
using EList.Models.ContactData;
using EList.Models.Conversations;
using EList.Models.EventOrganizators;
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
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => (UserNotificationType?)src.Type))
                .ForMember(dest => dest.Data, opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.Data) ? JsonConvert.DeserializeObject(src.Data) : null));
            CreateMap<Notification, NotificationDto>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => (int?)src.Type))
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
            CreateMap<OrganizationPayoutDto, OrganizationPayout>().ReverseMap();
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
        }
    }
}