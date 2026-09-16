using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Enums;
using EList.Models.Events;
using EList.Repositories.Interfaces;
using EList.Validators.Interfaces;

namespace EList.Validators.Impl
{
    public class EventAccessValidator : IEventAccessValidator
    {
        private readonly IEventsRepository _eventsRepository;
        private readonly IParticipationsRepository _participationsRepository;
        private readonly IInvitationsRepository _invitationsRepository;
        private readonly IEventOrganizatorsRepository _eventOrganizatorsRepository;
        private readonly IParticipantsBWListRepository _participantsBWListRepository;

        public EventAccessValidator(
            IEventsRepository eventsRepository,
            IParticipationsRepository participationsRepository,
            IInvitationsRepository invitationsRepository,
            IEventOrganizatorsRepository eventOrganizatorsRepository,
            IParticipantsBWListRepository participantsBWListRepository)
        {
            _eventsRepository = eventsRepository;
            _participationsRepository = participationsRepository;
            _invitationsRepository = invitationsRepository;
            _eventOrganizatorsRepository = eventOrganizatorsRepository;
            _participantsBWListRepository = participantsBWListRepository;
        }

        public async Task<bool> IsAccountEventOrganizatorAsync(Guid eventId, Guid accountId)
        {
            return await _eventOrganizatorsRepository.IsAccountEventOrganizatorAsync(eventId, accountId);
        }

        public async Task<CommandResult> AssertCanViewEventAsync(
            Guid eventId,
            Guid? viewerAccountId,
            bool adultConfirmed)
        {
            var eventItem = await _eventsRepository.GetEventAsync(eventId);
            if (eventItem == null)
                return CommandResult.Fail(ErrorCode.EventNotFound, $"Событие с id='{eventId}' не найдено");

            return await AssertCanViewEventAsync(eventItem, viewerAccountId, adultConfirmed);
        }

        public async Task<CommandResult> AssertCanViewEventAsync(
            Event eventItem,
            Guid? viewerAccountId,
            bool adultConfirmed,
            bool? isOrganizator = null)
        {
            var organizator = isOrganizator
                ?? (viewerAccountId != null
                    && await _eventOrganizatorsRepository.IsAccountEventOrganizatorAsync(eventItem.Id, viewerAccountId.Value));

            if (!organizator)
            {
                var privacyError = await AssertPrivacyAccessAsync(eventItem, viewerAccountId);
                if (!privacyError.Success)
                    return privacyError;

                var ageError = AssertAgeAccess(eventItem, adultConfirmed);
                if (!ageError.Success)
                    return ageError;
            }

            return CommandResult.OK;
        }

        public async Task<CommandResult> AssertCanJoinEventAsync(
            Event eventItem,
            Guid accountId,
            bool hasProvenInvitation = false)
        {
            if (eventItem.Parameters?.Private == true)
            {
                if (await _participantsBWListRepository.IsUserInWhiteListAsync(eventItem.Id, accountId))
                    return CommandResult.OK;

                var whiteListIsEmpty = await _participantsBWListRepository.IsWhiteListEmptyAsync(eventItem.Id);
                if (whiteListIsEmpty)
                {
                    if (hasProvenInvitation)
                        return CommandResult.OK;

                    var isUserInvited = await _invitationsRepository.IsUserInvitatedAsync(accountId, eventItem.Id);
                    if (isUserInvited)
                        return CommandResult.OK;

                    return CommandResult.Fail(
                        ErrorCode.AccessError,
                        "Принять участие в закрытом мероприятии можно только по приглашению");
                }

                return CommandResult.Fail(
                    ErrorCode.AccessError,
                    "Участвовать в закрытом мероприятии могут только пользователи из белого списка");
            }

            if (await _participantsBWListRepository.IsUserInBlackListAsync(eventItem.Id, accountId))
            {
                return CommandResult.Fail(
                    ErrorCode.AccessError,
                    "Организатор добавил вас в чёрный список мероприятия");
            }

            return CommandResult.OK;
        }

        private async Task<CommandResult> AssertPrivacyAccessAsync(Event eventItem, Guid? viewerAccountId)
        {
            var eventId = eventItem.Id;

            if (eventItem.Parameters?.Private == true)
            {
                if (viewerAccountId == null)
                    return CommandResult.Fail(ErrorCode.EventAccessDenied, "Сначала Необходимо авторизоваться");

                // Закрытое + человек в WL → видит всегда.
                if (await _participantsBWListRepository.IsUserInWhiteListAsync(eventId, viewerAccountId.Value))
                    return CommandResult.OK;

                var whiteListIsEmpty = await _participantsBWListRepository.IsWhiteListEmptyAsync(eventId);
                if (!whiteListIsEmpty)
                {
                    // WL не пуст и пользователя нет в нём — мероприятие скрыто
                    // (приглашать таких нельзя, см. InvitationsService.CreateAsync).
                    return CommandResult.Fail(
                        ErrorCode.EventAccessDenied,
                        "Это закрытое мероприятие доступно только участникам белого списка");
                }

                // WL пуст → только приглашённые / уже участвующие.
                var isUserParticipated = await _participationsRepository.IsUserParticipatedAsync(
                    viewerAccountId.Value, eventId);
                if (isUserParticipated)
                    return CommandResult.OK;

                var invitation = await _invitationsRepository.GetInvitationAsync(
                    viewerAccountId.Value, eventId);
                if (invitation == null)
                {
                    return CommandResult.Fail(
                        ErrorCode.EventAccessDenied,
                        "Посещать закрытые мероприятия можно только по приглашению");
                }

                return CommandResult.OK;
            }

            if (viewerAccountId != null)
            {
                var isUserInBlackList = await _participantsBWListRepository.IsUserInBlackListAsync(
                    eventId, viewerAccountId.Value);
                if (isUserInBlackList)
                {
                    return CommandResult.Fail(
                        ErrorCode.EventAccessDenied,
                        "Организатор добавил вас в чёрный список мероприятия");
                }
            }

            return CommandResult.OK;
        }

        private static CommandResult AssertAgeAccess(Event eventItem, bool adultConfirmed)
        {
            var ageLimit = GetEventMinAllowedAge(eventItem.Parameters?.AgeLimit);
            if (ageLimit >= 18 && !adultConfirmed)
                return CommandResult.Fail(ErrorCode.EventAccessDenied, "Просмотр мероприятий 18+ недоступен");

            return CommandResult.OK;
        }

        private static int GetEventMinAllowedAge(int? value)
        {
            value ??= 0;
            var ageRatingValues = Enum.GetValues<AgeRating>().Cast<int>().ToList();
            return ageRatingValues.FirstOrDefault(x => x >= value, 18);
        }
    }
}
