using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Media;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using NLog;
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

        public MediaService(ICorrelationIdProvider correlationIdProvider,
            IMediaRepository mediaRepository,
            IAccountDataHolder accountDataHolder,
            IParticipationsRepository participationsRepository,
            IEventOrganizatorsRepository eventOrganizatorsRepository)
        {
            _correlationIdProvider = correlationIdProvider;
            _mediaRepository = mediaRepository;
            _accountDataHolder = accountDataHolder;
            _participationsRepository = participationsRepository;
            _eventOrganizatorsRepository = eventOrganizatorsRepository;
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

            //var participants = await _participationsRepository.GetEventParticipantIdsAsync(eventId);
            //var organizators = await _eventOrganizatorsRepository.GetOrganizatorIdsByEventIdAsync(eventId);
            //if (!participants.Contains(_accountDataHolder.AccountId) && !organizators.Contains(_accountDataHolder.AccountId))
            //{
            //    result = result?.Where(i => i.Parameters.)
            //    //TODO: отобрать только те альбомы, которые доступны для просмотра
            //}

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<MediaAlbum>>(result);
        }

        public async Task<CommandResult<PagedList<AlbumFile>>> GetAlbumFilesAsync(Guid albumId, int? pageIndex = null, int? pageSize = null)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAlbumFilesAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _mediaRepository.GetAlbumFilesAsync(albumId);

            //if (authorizationInfo.AccountId != accountId)
            {
                //TODO: отобрать только те файлы, которые доступны для просмотра, с учетом доступности альбома
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

            await _mediaRepository.SetNewAccountAvatarAsync(_accountDataHolder.AccountId, fileId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<List<Guid>?>> GetCurAccountAvatarsAsync()
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetCurAccountAvatarsAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _mediaRepository.GetAccountAvatarsAsync(_accountDataHolder.AccountId);

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

            var result = await _mediaRepository.GetLastAccountAvatarAsync(_accountDataHolder.AccountId);

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
