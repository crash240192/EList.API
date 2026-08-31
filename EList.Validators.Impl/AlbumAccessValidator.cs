using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Events;
using EList.Models.Media;
using EList.Repositories.Interfaces;
using EList.Validators.Interfaces;

namespace EList.Validators.Impl
{
    public class AlbumAccessValidator : IAlbumAccessValidator
    {
        private readonly IEventsRepository _eventsRepository;
        private readonly IParticipationsRepository _participationsRepository;
        private readonly IInvitationsRepository _invitationsRepository;
        private readonly IEventOrganizatorsRepository _eventOrganizatorsRepository;
        private readonly IParticipantsBWListRepository _participantsBWListRepository;

        public AlbumAccessValidator(
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

        public AlbumAccessParameters ResolveParameters(MediaAlbum album)
        {
            if (album.EventId != null)
                return AlbumAccessParameters.FromEvent(album.Parameters);

            return AlbumAccessParameters.FromAccount(album.AccountParameters);
        }

        public async Task<CommandResult> AssertCanViewAlbumAsync(MediaAlbum album, Guid? viewerAccountId)
        {
            if (await CanViewAlbumAsync(album, viewerAccountId))
                return CommandResult.OK;

            return CommandResult.Fail(ErrorCode.AccessError, "Альбом недоступен для просмотра");
        }

        public async Task<CommandResult> AssertCanModifyAlbumAsync(
            MediaAlbum album,
            Guid? viewerAccountId,
            AlbumAccessOperation operation)
        {
            if (viewerAccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            if (album.EventId != null)
                return await AssertCanModifyEventAlbumAsync(album, viewerAccountId.Value, operation);

            return AssertCanModifyPersonalAlbum(album, viewerAccountId.Value, operation);
        }

        public async Task<List<MediaAlbum>> FilterViewableAlbumsAsync(
            IEnumerable<MediaAlbum> albums,
            Guid ownerAccountId,
            Guid? viewerAccountId)
        {
            var result = new List<MediaAlbum>();
            foreach (var album in albums)
            {
                if (await CanViewAlbumAsync(album, viewerAccountId))
                    result.Add(album);
            }

            return result;
        }

        private async Task<bool> CanViewAlbumAsync(MediaAlbum album, Guid? viewerAccountId)
        {
            if (viewerAccountId == album.AccountId)
                return true;

            if (album.EventId != null)
                return await CanViewEventAlbumAsync(album, viewerAccountId);

            return CanViewPersonalAlbum(album, viewerAccountId);
        }

        private async Task<bool> CanViewEventAlbumAsync(MediaAlbum album, Guid? viewerAccountId)
        {
            var eventId = album.EventId!.Value;
            var access = await LoadEventAccessAsync(eventId, viewerAccountId);
            if (access == null)
                return false;

            if (access.IsOrganizator)
                return true;

            var parameters = ResolveParameters(album);
            var isParticipantOrInvited = viewerAccountId != null
                && (access.Participants.Contains(viewerAccountId.Value)
                    || access.InvitedUsers.Contains(viewerAccountId.Value));

            if (access.Event.Parameters?.Private == true)
            {
                if (!await HasPrivateEventViewAccessAsync(access, viewerAccountId))
                    return false;

                if (parameters.Private && !isParticipantOrInvited)
                    return false;

                return true;
            }

            if (viewerAccountId != null
                && await _participantsBWListRepository.IsUserInBlackListAsync(eventId, viewerAccountId.Value))
                return false;

            if (parameters.Private)
                return isParticipantOrInvited;

            return true;
        }

        private static bool CanViewPersonalAlbum(MediaAlbum album, Guid? viewerAccountId)
        {
            var parameters = AlbumAccessParameters.FromAccount(album.AccountParameters);
            if (!parameters.Private)
                return true;

            return viewerAccountId == album.AccountId;
        }

        private async Task<CommandResult> AssertCanModifyEventAlbumAsync(
            MediaAlbum album,
            Guid viewerAccountId,
            AlbumAccessOperation operation)
        {
            var eventId = album.EventId!.Value;
            var access = await LoadEventAccessAsync(eventId, viewerAccountId);
            if (access == null)
                return CommandResult.Fail(ErrorCode.EventNotFound, "Событие альбома не найдено");

            var isOrganizator = access.IsOrganizator;
            var isOwner = album.AccountId == viewerAccountId;
            var parameters = ResolveParameters(album);

            switch (operation)
            {
                case AlbumAccessOperation.View:
                    return await AssertCanViewAlbumAsync(album, viewerAccountId);

                case AlbumAccessOperation.Delete:
                    if (isOrganizator || isOwner)
                        return CommandResult.OK;
                    return CommandResult.Fail(ErrorCode.AccessError, "Удалить альбом может только организатор мероприятия или его владелец");

                case AlbumAccessOperation.ModifyMetadata:
                case AlbumAccessOperation.Assign:
                    if (isOrganizator || isOwner)
                        return CommandResult.OK;
                    return CommandResult.Fail(ErrorCode.AccessError, "Изменять альбом может только организатор мероприятия или его владелец");

                case AlbumAccessOperation.AddFiles:
                    if (isOrganizator || isOwner)
                        return CommandResult.OK;

                    if (!await HasEventViewAccessAsync(access, viewerAccountId))
                        return CommandResult.Fail(ErrorCode.AccessError, "Альбом доступен только участникам мероприятия");

                    if (parameters.ParticipantsReadonly)
                        return CommandResult.Fail(ErrorCode.AddPhotosNotAllowed, "Организатор запретил добавление фотографий в этот альбом");

                    return CommandResult.OK;

                default:
                    return CommandResult.Fail(ErrorCode.AccessError, "Операция недоступна");
            }
        }

        private static CommandResult AssertCanModifyPersonalAlbum(
            MediaAlbum album,
            Guid viewerAccountId,
            AlbumAccessOperation operation)
        {
            if (album.AccountId != viewerAccountId)
                return CommandResult.Fail(ErrorCode.AccessError, "Изменять альбом может только его владелец");

            var parameters = AlbumAccessParameters.FromAccount(album.AccountParameters);
            if (operation == AlbumAccessOperation.AddFiles && parameters.ParticipantsReadonly)
                return CommandResult.Fail(ErrorCode.AddPhotosNotAllowed, "Добавление фотографий в этот альбом запрещено");

            return CommandResult.OK;
        }

        private async Task<bool> HasEventViewAccessAsync(EventAccessSnapshot access, Guid? viewerAccountId)
        {
            if (access.Event.Parameters?.Private == true)
                return await HasPrivateEventViewAccessAsync(access, viewerAccountId);

            if (viewerAccountId != null
                && await _participantsBWListRepository.IsUserInBlackListAsync(access.Event.Id, viewerAccountId.Value))
                return false;

            return viewerAccountId != null
                && (access.Participants.Contains(viewerAccountId.Value)
                    || access.InvitedUsers.Contains(viewerAccountId.Value));
        }

        private async Task<bool> HasPrivateEventViewAccessAsync(EventAccessSnapshot access, Guid? viewerAccountId)
        {
            if (viewerAccountId == null)
                return false;

            var isUserInWhiteList = await _participantsBWListRepository.IsUserInWhiteListAsync(
                access.Event.Id, viewerAccountId.Value);
            if (!isUserInWhiteList)
            {
                var whiteListIsEmpty = await _participantsBWListRepository.IsWhiteListEmptyAsync(access.Event.Id);
                if (whiteListIsEmpty)
                {
                    if (access.Participants.Contains(viewerAccountId.Value))
                        return true;

                    var invitation = await _invitationsRepository.GetInvitationAsync(viewerAccountId.Value, access.Event.Id);
                    return invitation != null;
                }

                return false;
            }

            return access.Participants.Contains(viewerAccountId.Value)
                || access.InvitedUsers.Contains(viewerAccountId.Value);
        }

        private async Task<EventAccessSnapshot?> LoadEventAccessAsync(Guid eventId, Guid? viewerAccountId)
        {
            var eventItem = await _eventsRepository.GetEventAsync(eventId);
            if (eventItem == null)
                return null;

            var participants = await _participationsRepository.GetEventParticipantIdsAsync(eventId);
            var invitedUsers = await _invitationsRepository.GetInvitedUsersAsync(eventId);
            var isOrganizator = viewerAccountId != null
                && await _eventOrganizatorsRepository.IsAccountEventOrganizatorAsync(eventId, viewerAccountId.Value);

            return new EventAccessSnapshot
            {
                Event = eventItem,
                IsOrganizator = isOrganizator,
                Participants = participants?.ToHashSet() ?? new HashSet<Guid>(),
                InvitedUsers = invitedUsers?.ToHashSet() ?? new HashSet<Guid>()
            };
        }

        private sealed class EventAccessSnapshot
        {
            public Event Event { get; init; }
            public bool IsOrganizator { get; init; }
            public HashSet<Guid> Participants { get; init; }
            public HashSet<Guid> InvitedUsers { get; init; }
        }
    }
}
