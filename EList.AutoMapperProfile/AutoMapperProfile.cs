using AutoMapper;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;
using EList.Models.Accounts;
using EList.Models.Authorization;
using EList.Models.ContactData;
using EList.Models.Enums;
using EList.Models.EventOrganizators;
using EList.Models.Events;
using EList.Models.Events.EventMetadata;
using EList.Models.Invitations;
using EList.Models.Media;
using EList.Models.Notifications;
using EList.Models.Person;
using EList.Models.Subscriptions;
using EList.Models.EventsRating;
using EList.Models.Wallets;
using EList.Models.Participation;

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

            //CreateMap<StartNewProcessRequest, StartNewProcessDto>()
            //    .ForMember(x => x.RoleContext, o =>
            //        o.MapFrom(s => s.RoleContext));

            CreateMap<PersonInfoDto, PersonInfo>().ReverseMap();
            CreateMap<ContactTypeDto, ContactType>().ReverseMap();
            CreateMap<ContactDataDto, ContactDataItem>().ReverseMap();
            CreateMap<AuthorizationDto, Authorization>().ReverseMap();
            CreateMap<AccountDto, Account>().ReverseMap();
            CreateMap<SystemNotificationDto, SystemNotification>().ReverseMap();
            CreateMap<SubscriptionDto, Subscription>().ReverseMap();
            CreateMap<EventCategoryDto, EventCategory>().ReverseMap();
            CreateMap<EventTypeDto, EventType>().ReverseMap();
            CreateMap<EventParametersDto, EventParameters>().ReverseMap();
            CreateMap<EventDto, Event>().ReverseMap();
            CreateMap<MediaAlbumDto, MediaAlbum>().ReverseMap();
            CreateMap<InvitationDto, Invitation>().ReverseMap();
            CreateMap<EventsRatingDto, EventsRating>().ReverseMap();

            CreateMap<TariffDto, Tariff>().ReverseMap();
            CreateMap<TariffValidatorDto, TariffValidator>().ReverseMap();
            CreateMap<WalletDto, Wallet>().ReverseMap();

            CreateMap<AccountDto, CreateAccountRequest>().ReverseMap();
            CreateMap<EventOrganizatorDto, EventOrganizator>().ReverseMap();

            CreateMap<EventParametersRequest, SetEventParametersRequest>().ReverseMap();
            CreateMap<EventsSearchRequest, DbDataProvider.Models.SearchRequests.EventsSearchRequest>().ReverseMap();
            CreateMap<EventParticipantsSearchRequest, DbDataProvider.Models.SearchRequests.EventParticipantsSearchRequest>().ReverseMap();

            //CreateMap<(Account, PersonInfo),>
            CreateMap<Models.Enums.Gender, DbDataProvider.Models.Enums.Gender>().ReverseMap();
            CreateMap<Models.Enums.SystemNotificationType, DbDataProvider.Models.Enums.SystemNotificationType>().ReverseMap();
        }
    }
}