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
using EList.Models.Participation;
using EList.Models.Person;
using EList.Models.Subscriptions;
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

            CreateMap<AuthorizationDto, AuthorizationResponse>().ReverseMap();
            CreateMap<AuthorizationDto, Authorization>().ReverseMap();

            CreateMap<AccountDto, CreateAccountRequest>().ReverseMap();

            CreateMap<AccountDto, Account>();
            CreateMap<Account, AccountDto>()
                .ForMember(dest => dest.Avatars, opt => opt.Ignore());

            CreateMap<AccountDto, AccountPublicData>();

            CreateMap<PersonInfoDto, PersonInfo>().ReverseMap();

            CreateMap<ContactTypeDto, ContactType>().ReverseMap();
            CreateMap<ContactDataDto, ContactDataItem>().ReverseMap();

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

            CreateMap<MessageDto, Message>().ReverseMap();
            CreateMap<MessageRequest, MessageDto>().ReverseMap();
            CreateMap<ConversationDto, Conversation>().ReverseMap();
            CreateMap<ConversationRequest, ConversationDto>().ReverseMap();

            CreateMap<Models.Enums.Gender, DbDataProvider.Models.Enums.Gender>().ReverseMap();
            CreateMap<Models.Enums.SystemNotificationType, DbDataProvider.Models.Enums.SystemNotificationType>().ReverseMap();
            CreateMap<Models.Enums.EventRatingType, DbDataProvider.Models.Enums.EventRatingType>().ReverseMap();
        }
    }
}