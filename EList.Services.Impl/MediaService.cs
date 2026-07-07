using EList.Common.CorrelationId;
using EList.Common.Extensions;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Common.Threading;
using EList.FilestorageClient;
using EList.Models.Accounts;
using EList.Models.Events;
using EList.Models.Media;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using NLog;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace EList.Services.Impl
{
    public class MediaService : IMediaService
    {
        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.MediaService.";
        #endregion

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IMediaRepository _mediaRepository;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly IParticipationsRepository _participationsRepository;
        private readonly IEventOrganizatorsRepository _eventOrganizatorsRepository;
        private readonly IEventsRepository _eventsRepository;
        private readonly IInvitationsRepository _invitationsRepository;
        private readonly IFilestorageClient _filestorageClient;
        public MediaService(ICorrelationIdProvider correlationIdProvider,
            IMediaRepository mediaRepository,
            IAccountDataHolder accountDataHolder,
            IParticipationsRepository participationsRepository,
            IEventOrganizatorsRepository eventOrganizatorsRepository,
            IEventsRepository eventsRepository,
            IInvitationsRepository invitationsRepository,
            IFilestorageClient filestorageClient)
        {
            _correlationIdProvider = correlationIdProvider;
            _mediaRepository = mediaRepository;
            _accountDataHolder = accountDataHolder;
            _participationsRepository = participationsRepository;
            _eventOrganizatorsRepository = eventOrganizatorsRepository;
            _eventsRepository = eventsRepository;
            _invitationsRepository = invitationsRepository;
            _filestorageClient = filestorageClient;
        }

        public async Task<CommandResult<Guid?>> CreateAlbumAsync(EventAlbumRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateAlbumAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);
            request.AccountId = _accountDataHolder.AccountId;
            var result = await _mediaRepository.CreateAlbumAsync(request);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid?>(result);
        }

        public async Task<CommandResult> UpdateAlbumAsync(EventAlbumRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateAlbumAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            //TODO: Добавить проверку что пользователь может редактировать альбом

            await _mediaRepository.UpdateAlbumAsync(request);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> AssingAlbumToEventAsync(Guid eventId, Guid albumId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AssingAlbumToEventAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            //TODO: Добавить проверку что пользователь может редактировать альбом

            await _mediaRepository.AssingAlbumToEventAsync(eventId, albumId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> AssingAlbumToAccountAsync(Guid accountId, Guid albumId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AssingAlbumToAccountAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            //TODO: Добавить проверку что пользователь может редактировать альбом

            await _mediaRepository.AssingAlbumToAccountAsync(accountId, albumId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> AddFilesToAlbumAsync(AddFilesRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AddFilesToAlbumAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var album = await _mediaRepository.GetAlbumAsync(request.AlbumId);

            if (album == null)
                return CommandResult.Fail(ErrorCode.AlbumNotFound, $"Альбом {request.AlbumId} не найден");

            if (!request.FileIds?.Any() ?? true)
                return CommandResult.Fail(ErrorCode.AlbumNotFound, $"Перечень файлов не должен быть пустым");

            if (album.EventId != null)
            {
                var organizators = await _eventOrganizatorsRepository.GetOrganizatorIdsByEventIdAsync(album.EventId.Value);
                if (!organizators.Contains(_accountDataHolder.AccountId.Value))
                {
                    var eventItem = await _eventsRepository.GetEventAsync(album.EventId.Value);
                    var participants = await _participationsRepository.GetEventParticipantIdsAsync(album.EventId.Value);
                    var invitedUsers = await _invitationsRepository.GetInvitedUsersAsync(album.EventId.Value);

                    if (eventItem?.Parameters?.Private ?? false)
                    {
                        if (!participants.Contains(_accountDataHolder.AccountId.Value) && !invitedUsers.Contains(_accountDataHolder.AccountId.Value))
                        {
                            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                            return CommandResult<PagedList<AlbumFile>>.Fail(ErrorCode.AccessError, "Альбом доступен только участникам мероприятия");
                        }
                    }
                    else
                    {
                        if (album.Parameters?.Private ?? false)
                        {
                            if (!participants.Contains(_accountDataHolder.AccountId.Value) && !invitedUsers.Contains(_accountDataHolder.AccountId.Value))
                            {
                                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                                return CommandResult<PagedList<AlbumFile>>.Fail(ErrorCode.AccessError, "Альбом доступен для просмотра только участникам мероприятия");
                            }
                        }
                    }

                    if (album.Parameters?.ParticipantsReadonly ?? false)
                    {
                        logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                        return CommandResult.Fail(ErrorCode.AddPhotosNotAllowed, "Организатор запретил добавление фотографий в этот альбом");
                    }
                }
            }

            //TODO: Добавить проверку доступа для добавления файлов в альбом без привязки к мероприятию

            await _mediaRepository.AddFilesToAlbumAsync(request.AlbumId, request.FileIds);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<List<MediaAlbum>>> GetAccountAlbumsAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAccountAlbumsAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _mediaRepository.GetAccountAlbumsAsync(accountId);

            if (_accountDataHolder.AccountId != accountId)
            {
                //TODO: отобрать только те альбомы, которые доступны для просмотра
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<MediaAlbum>>(result);
        }

        public async Task<CommandResult<MediaAlbum>> GetAlbumAsync(Guid id)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAlbumAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _mediaRepository.GetAlbumAsync(id);

            if (_accountDataHolder.AccountId != result.AccountId)
            {
                //TODO: Проверить альбом на доступность просмотра
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<MediaAlbum>(result);
        }

        public async Task<CommandResult<List<MediaAlbum>>> GetEventAlbumsAsync(Guid eventId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetEventAlbumsAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _mediaRepository.GetEventAlbumsAsync(eventId);
            var eventItem = await _eventsRepository.GetEventAsync(eventId);
            var participants = await _participationsRepository.GetEventParticipantIdsAsync(eventId);
            var organizators = await _eventOrganizatorsRepository.GetOrganizatorIdsByEventIdAsync(eventId);
            var invitedUsers = await _invitationsRepository.GetInvitedUsersAsync(eventId);

            if (_accountDataHolder.AccountId != null && organizators.Contains(_accountDataHolder.AccountId.Value))
            {
                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return new CommandResult<List<MediaAlbum>>(result);
            }

            if (eventItem.Parameters?.Private ?? false)
            {
                if (_accountDataHolder.AccountId == null || (!participants.Contains(_accountDataHolder.AccountId.Value) && !invitedUsers.Contains(_accountDataHolder.AccountId.Value)))
                {
                    logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                    return CommandResult<List<MediaAlbum>>.Fail(ErrorCode.AccessError, "Просмотр альбомов закрытого мероприятия доступен только участникам");
                }
            }
            else
            {
                if (_accountDataHolder.AccountId == null || (!participants.Contains(_accountDataHolder.AccountId.Value) && !invitedUsers.Contains(_accountDataHolder.AccountId.Value)))
                {
                    result = result?.Where(i => !i.Parameters?.Private ?? true)?.ToList();
                }
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<MediaAlbum>>(result);
        }

        public async Task<CommandResult<PagedList<EventAlbumsContainer>>> GetEventsAlbumsAsync(Guid accountId, int? pageIndex = null, int? pageSize = null)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetEventsAlbumsAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _mediaRepository.GetEventsAlbumsAsync(accountId, _accountDataHolder.AccountId, pageIndex, pageSize);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<EventAlbumsContainer>>(result);
        }


        public async Task<CommandResult> DeleteAlbumAsync(Guid albumId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteAlbumAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var album = await _mediaRepository.GetAlbumAsync(albumId);

            if (album == null)
                return CommandResult.Fail(ErrorCode.AlbumNotFound, "Альбом не найден");

            if (album.EventId != null)
            {
                var organizators = await _eventOrganizatorsRepository.GetOrganizatorIdsByEventIdAsync(album.EventId.Value);
                if (!organizators.Contains(_accountDataHolder.AccountId.Value) && _accountDataHolder.AccountId != album.AccountId)
                {
                    logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                    return CommandResult<List<MediaAlbum>>.Fail(ErrorCode.AccessError, "Удалить альбом может только организатор мероприятия");
                }
            }
            else
            {
                if (_accountDataHolder.AccountId != album.AccountId)
                {
                    logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                    return CommandResult<List<MediaAlbum>>.Fail(ErrorCode.AccessError, "Удалить альбом может только его владелец");
                }
            }

            var files = await _mediaRepository.GetAlbumFilesAsync(albumId);

            if (files.Result.NullSafeAny())
            {
                var fileIds = files.Result?.Select(i => i.Id).ToList();
                //TODO: Добавить проверку что этот файл не является в том числе аватаркой 
                var filesWithoutAlbums = await _mediaRepository.GetFilesNotExistsInAnotherAlbumsAsync(fileIds, albumId);

                //Тут мы проверяем что этот файл больше не прикреплён ни к одному альбому
                if (filesWithoutAlbums.NullSafeAny())
                {
                    var fileIdsConcurrentQueue = new ConcurrentQueue<Guid>(filesWithoutAlbums);

                    var tasks = new List<Task>();
                    for (int i = 0; i < 10; i++)
                    {
                        var task = Task.Run(async () =>
                        {
                            while (fileIdsConcurrentQueue.TryDequeue(out var curFileId))
                            {
                                await _filestorageClient.DeleteFileAsync(curFileId, _accountDataHolder.Token.Value, _accountDataHolder.Jwt);
                            }
                        });
                    }

                    Task.WaitAll(tasks.ToArray());
                }
            }

            await _mediaRepository.DeleteAlbumAsync(albumId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> DeleteFileAsync(Guid fileId, Guid albumId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteFileAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var file = await _mediaRepository.GetFileAsync(fileId, albumId);
            if (file == null)
            {
                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return CommandResult.Fail(ErrorCode.AlbumItemNotFound, "Файл не найден");
            }

            var album = await _mediaRepository.GetAlbumAsync(albumId);
            if (album == null)
            {
                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return CommandResult.Fail(ErrorCode.AlbumNotFound, "Альбом не найден");
            }

            if (album.EventId != null)
            {
                var organizators = await _eventOrganizatorsRepository.GetOrganizatorIdsByEventIdAsync(album.EventId.Value);
                if (!organizators.Contains(_accountDataHolder.AccountId.Value) && _accountDataHolder.AccountId != album.AccountId)
                {
                    logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                    return CommandResult<List<MediaAlbum>>.Fail(ErrorCode.AccessError, "Удалить альбом может только организатор мероприятия");
                }
            }
            else
            {
                if (_accountDataHolder.AccountId != album.AccountId)
                {
                    logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                    return CommandResult<List<MediaAlbum>>.Fail(ErrorCode.AccessError, "Удалить альбом может только его владелец");
                }
            }

            //TODO: Добавить првоерку что файл не является в том числе аватаркой 
            var filesWithoutAlbums = await _mediaRepository.GetFilesNotExistsInAnotherAlbumsAsync(new List<Guid> { fileId }, albumId);

            if (filesWithoutAlbums.NullSafeAny())
                await _filestorageClient.DeleteFileAsync(filesWithoutAlbums.FirstOrDefault(), _accountDataHolder.Token.Value, _accountDataHolder.Jwt);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }


        public async Task<CommandResult<PagedList<AlbumFile>>> GetAlbumFilesAsync(Guid albumId, int? pageIndex = null, int? pageSize = null)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAlbumFilesAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var album = await _mediaRepository.GetAlbumAsync(albumId);

            if (album.EventId != null)
            {
                var organizators = await _eventOrganizatorsRepository.GetOrganizatorIdsByEventIdAsync(album.EventId.Value);

                if (_accountDataHolder.AccountId == null || !organizators.Contains(_accountDataHolder.AccountId.Value))
                {
                    var eventItem = await _eventsRepository.GetEventAsync(album.EventId.Value);
                    var participants = await _participationsRepository.GetEventParticipantIdsAsync(album.EventId.Value);
                    var invitedUsers = await _invitationsRepository.GetInvitedUsersAsync(album.EventId.Value);

                    if (eventItem?.Parameters?.Private ?? false)
                    {
                        if (_accountDataHolder.AccountId == null || (!participants.Contains(_accountDataHolder.AccountId.Value) && !invitedUsers.Contains(_accountDataHolder.AccountId.Value)))
                        {
                            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                            return CommandResult<PagedList<AlbumFile>>.Fail(ErrorCode.AccessError, "Просмотр альбомов закрытого мероприятия доступен только участникам");
                        }
                    }
                    else
                    {
                        if (album.Parameters?.Private ?? false)
                        {
                            if (_accountDataHolder.AccountId == null || (!participants.Contains(_accountDataHolder.AccountId.Value) && !invitedUsers.Contains(_accountDataHolder.AccountId.Value)))
                            {
                                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                                return CommandResult<PagedList<AlbumFile>>.Fail(ErrorCode.AccessError, "Альбом доступен для просмотра только участникам мероприятия");
                            }
                        }
                    }
                }
            }

            var result = await _mediaRepository.GetAlbumFilesAsync(albumId);
            {
                //TODO: отобрать только те файлы, которые доступны для просмотра, с учетом доступности альбома другим пользователям
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<AlbumFile>>(result);
        }

        //public async Task<CommandResult> SetEventAlbumParametersAsync(Guid token, EventAlbumParameters request)
        //{
        //    var correlationId = _correlationIdProvider.Get();
        //    var execTime = Stopwatch.StartNew();
        //    var methodName = $"{LOGGER_NAME}{nameof(SetEventAlbumParametersAsync)}";
        //    logger.Debug(correlationId, null, methodName, $"Method started", null);

        //    var authorizationInfo = await _authorizationRepository.GetAuthorizationDataAsync(token);

        //    var result = await _mediaRepository.SetEventAlbumParametersAsync(request);

        //    //if (authorizationInfo.AccountId != accountId)
        //    {
        //        //TODO: отобрать только те альбомы, которые доступны для просмотра
        //    }

        //    logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
        //    return new CommandResult<List<MediaAlbum>>(result);
        //}

        #region account avatars
        public async Task<CommandResult> SetNewAccountAvatarAsync(Guid fileId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SetNewAccountAvatarAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            await _mediaRepository.SetNewAccountAvatarAsync(_accountDataHolder.AccountId.Value, fileId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<List<Guid>?>> GetCurAccountAvatarsAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetCurAccountAvatarsAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _mediaRepository.GetAccountAvatarsAsync(_accountDataHolder.AccountId.Value);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);

            if (result?.Any() ?? false)
                return new CommandResult<List<Guid>?>(result);

            return CommandResult<List<Guid>?>.OK(result);
        }

        public async Task<CommandResult<List<Guid>?>> GetAccountAvatarsAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAccountAvatarsAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _mediaRepository.GetAccountAvatarsAsync(accountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);

            return CommandResult<List<Guid>?>.OK(result);
        }

        public async Task<CommandResult<Guid?>> GetCurAccountAvatarAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetCurAccountAvatarAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _mediaRepository.GetLastAccountAvatarAsync(_accountDataHolder.AccountId.Value);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);

            return CommandResult<Guid?>.OK(result);
        }

        public async Task<CommandResult<Guid?>> GetAccountAvatarAsync(Guid accountId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAccountAvatarAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _mediaRepository.GetLastAccountAvatarAsync(accountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);

            return CommandResult<Guid?>.OK(result);
        }
        #endregion

        #region organization avatars
        public async Task<CommandResult> SetNewOrganizationAvatarAsync(Guid organizationId, Guid fileId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SetNewOrganizationAvatarAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var curAccountId = _accountDataHolder.AccountId;

            await _mediaRepository.SetNewOrganizationAvatarAsync(organizationId, fileId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<List<Guid>?>> GetOrganizationAvatarsAsync(Guid organizationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetOrganizationAvatarsAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _mediaRepository.GetOrganizationAvatarsAsync(organizationId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);

            if (result?.Any() ?? false)
                return new CommandResult<List<Guid>?>(result);

            return CommandResult<List<Guid>?>.OK(result);
        }

        public async Task<CommandResult<Guid?>> GetOrganizationAvatarAsync(Guid organizationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetOrganizationAvatarAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _mediaRepository.GetLastOrganizationAvatarAsync(organizationId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);

            if (result != null)
                return new CommandResult<Guid?>(result);

            return CommandResult<Guid?>.OK(result);
        }
        #endregion
    }
}
