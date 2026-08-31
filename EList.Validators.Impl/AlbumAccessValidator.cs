using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Media;
using EList.Repositories.Interfaces;
using EList.Validators.Interfaces;

namespace EList.Validators.Impl
{
    public class AlbumAccessValidator : IAlbumAccessValidator
    {
        private readonly IEventAccessValidator _eventAccessValidator;
        private readonly IEventsRepository _eventsRepository;
        private readonly IParticipationsRepository _participationsRepository;
        private readonly IInvitationsRepository _invitationsRepository;

        public AlbumAccessValidator(
            IEventAccessValidator eventAccessValidator,
            IEventsRepository eventsRepository,
            IParticipationsRepository participationsRepository,
            IInvitationsRepository invitationsRepository)
        {
            _eventAccessValidator = eventAccessValidator;
            _eventsRepository = eventsRepository;
            _participationsRepository = participationsRepository;
            _invitationsRepository = invitationsRepository;
        }

        public AlbumAccessParameters ResolveParameters(MediaAlbum album)
        {
            // Приоритет у привязки к событию: account_album_parameters не участвуют.
            if (album.EventId != null)
                return AlbumAccessParameters.FromEvent(album.Parameters);

            return AlbumAccessParameters.FromAccount(album.AccountParameters);
        }

        public async Task<CommandResult> AssertCanViewAlbumAsync(
            MediaAlbum album,
            Guid? viewerAccountId,
            bool adultConfirmed)
        {
            if (album.EventId != null)
            {
                var eventId = album.EventId.Value;
                var eventItem = await _eventsRepository.GetEventAsync(eventId);
                if (eventItem == null)
                    return CommandResult.Fail(ErrorCode.EventNotFound, "Событие альбома не найдено");

                var isOrganizator = viewerAccountId != null
                    && await _eventAccessValidator.IsAccountEventOrganizatorAsync(eventId, viewerAccountId.Value);

                var eventAccess = await _eventAccessValidator.AssertCanViewEventAsync(
                    eventItem, viewerAccountId, adultConfirmed, isOrganizator);
                if (!eventAccess.Success)
                    return eventAccess;

                if (isOrganizator)
                    return CommandResult.OK;

                var parameters = ResolveParameters(album);
                if (parameters.Private && !await IsParticipantOrInvitedAsync(eventId, viewerAccountId))
                    return CommandResult.Fail(ErrorCode.AccessError, "Альбом доступен для просмотра только участникам мероприятия");

                return CommandResult.OK;
            }

            if (viewerAccountId == album.AccountId || CanViewPersonalAlbum(album, viewerAccountId))
                return CommandResult.OK;

            return CommandResult.Fail(ErrorCode.AccessError, "Альбом недоступен для просмотра");
        }

        public async Task<CommandResult> AssertCanModifyAlbumAsync(
            MediaAlbum album,
            Guid? viewerAccountId,
            bool adultConfirmed,
            AlbumAccessOperation operation)
        {
            if (viewerAccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            if (album.EventId != null)
                return await AssertCanModifyEventAlbumAsync(album, viewerAccountId.Value, adultConfirmed, operation);

            return AssertCanModifyPersonalAlbum(album, viewerAccountId.Value, operation);
        }

        public async Task<List<MediaAlbum>> FilterViewableAlbumsAsync(
            IEnumerable<MediaAlbum> albums,
            Guid? viewerAccountId,
            bool adultConfirmed)
        {
            var result = new List<MediaAlbum>();
            foreach (var album in albums)
            {
                var access = await AssertCanViewAlbumAsync(album, viewerAccountId, adultConfirmed);
                if (access.Success)
                    result.Add(album);
            }

            return result;
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
            bool adultConfirmed,
            AlbumAccessOperation operation)
        {
            var eventId = album.EventId!.Value;
            var eventItem = await _eventsRepository.GetEventAsync(eventId);
            if (eventItem == null)
                return CommandResult.Fail(ErrorCode.EventNotFound, "Событие альбома не найдено");

            var isOrganizator = await _eventAccessValidator.IsAccountEventOrganizatorAsync(eventId, viewerAccountId);
            var isOwner = album.AccountId == viewerAccountId;
            var parameters = ResolveParameters(album);

            switch (operation)
            {
                case AlbumAccessOperation.View:
                    return await AssertCanViewAlbumAsync(album, viewerAccountId, adultConfirmed);

                case AlbumAccessOperation.Delete:
                case AlbumAccessOperation.ModifyMetadata:
                case AlbumAccessOperation.Assign:
                    if (isOrganizator || isOwner)
                        return CommandResult.OK;
                    return CommandResult.Fail(
                        ErrorCode.AccessError,
                        "Изменять альбом может только организатор мероприятия или его владелец");

                case AlbumAccessOperation.AddFiles:
                    if (isOrganizator || isOwner)
                        return CommandResult.OK;

                    var eventAccess = await _eventAccessValidator.AssertCanViewEventAsync(
                        eventItem, viewerAccountId, adultConfirmed, isOrganizator: false);
                    if (!eventAccess.Success)
                        return eventAccess;

                    if (parameters.Private && !await IsParticipantOrInvitedAsync(eventId, viewerAccountId))
                        return CommandResult.Fail(ErrorCode.AccessError, "Альбом доступен только участникам мероприятия");

                    if (parameters.ParticipantsReadonly)
                        return CommandResult.Fail(
                            ErrorCode.AddPhotosNotAllowed,
                            "Организатор запретил добавление фотографий в этот альбом");

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

        private async Task<bool> IsParticipantOrInvitedAsync(Guid eventId, Guid? viewerAccountId)
        {
            if (viewerAccountId == null)
                return false;

            if (await _participationsRepository.IsUserParticipatedAsync(viewerAccountId.Value, eventId))
                return true;

            var invitation = await _invitationsRepository.GetInvitationAsync(viewerAccountId.Value, eventId);
            return invitation != null;
        }
    }
}
