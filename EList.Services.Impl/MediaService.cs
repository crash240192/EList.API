using EList.Common.CorrelationId;
using EList.Common.Extensions;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Common.Threading;
using EList.FilestorageClient;
using EList.Models.Accounts;
using EList.Models.Media;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using EList.Validators.Interfaces;
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
        private readonly IEventOrganizatorsRepository _eventOrganizatorsRepository;
        private readonly IOrganizationsRepository _organizationsRepository;
        private readonly IAlbumAccessValidator _albumAccessValidator;
        private readonly IMediaAlbumValidator _mediaAlbumValidator;
        private readonly IFilestorageClient _filestorageClient;

        public MediaService(ICorrelationIdProvider correlationIdProvider,
            IMediaRepository mediaRepository,
            IAccountDataHolder accountDataHolder,
            IEventOrganizatorsRepository eventOrganizatorsRepository,
            IOrganizationsRepository organizationsRepository,
            IAlbumAccessValidator albumAccessValidator,
            IMediaAlbumValidator mediaAlbumValidator,
            IFilestorageClient filestorageClient)
        {
            _correlationIdProvider = correlationIdProvider;
            _mediaRepository = mediaRepository;
            _accountDataHolder = accountDataHolder;
            _eventOrganizatorsRepository = eventOrganizatorsRepository;
            _organizationsRepository = organizationsRepository;
            _albumAccessValidator = albumAccessValidator;
            _mediaAlbumValidator = mediaAlbumValidator;
            _filestorageClient = filestorageClient;
        }

        public async Task<CommandResult<Guid?>> CreateAlbumAsync(EventAlbumRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateAlbumAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var albumError = _mediaAlbumValidator.ValidateAlbumRequest(request);
            if (!albumError.Success)
                return CommandResult<Guid?>.Fail(albumError.ErrorCode, albumError.Message);

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

            if (request.Id == null)
                return CommandResult.Fail(ErrorCode.AlbumNotFound, "Не указан идентификатор альбома");

            var albumError = _mediaAlbumValidator.ValidateAlbumRequest(request, requireName: false);
            if (!albumError.Success)
                return albumError;

            var album = await _mediaRepository.GetAlbumAsync(request.Id.Value);
            if (album == null)
                return CommandResult.Fail(ErrorCode.AlbumNotFound, "Альбом не найден");

            var accessError = await _albumAccessValidator.AssertCanModifyAlbumAsync(
                album, _accountDataHolder.AccountId, _accountDataHolder.AdultConfirmed, AlbumAccessOperation.ModifyMetadata);
            if (!accessError.Success)
                return accessError;

            await _mediaRepository.UpdateAlbumAsync(request);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> AssignAlbumToEventAsync(Guid eventId, Guid albumId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AssignAlbumToEventAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var album = await _mediaRepository.GetAlbumAsync(albumId);
            if (album == null)
                return CommandResult.Fail(ErrorCode.AlbumNotFound, "Альбом не найден");

            var accessError = await _albumAccessValidator.AssertCanModifyAlbumAsync(
                album, _accountDataHolder.AccountId, _accountDataHolder.AdultConfirmed, AlbumAccessOperation.Assign);
            if (!accessError.Success)
                return accessError;

            if (!await _eventOrganizatorsRepository.IsAccountEventOrganizatorAsync(eventId, _accountDataHolder.AccountId.Value))
                return CommandResult.Fail(ErrorCode.AccessError, "Привязать альбом к мероприятию может только организатор");

            await _mediaRepository.AssignAlbumToEventAsync(eventId, albumId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> AssignAlbumToAccountAsync(Guid accountId, Guid albumId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AssignAlbumToAccountAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            if (_accountDataHolder.AccountId != accountId)
                return CommandResult.Fail(ErrorCode.AccessError, "Альбом можно привязать только к своему аккаунту");

            var album = await _mediaRepository.GetAlbumAsync(albumId);
            if (album == null)
                return CommandResult.Fail(ErrorCode.AlbumNotFound, "Альбом не найден");

            var accessError = await _albumAccessValidator.AssertCanModifyAlbumAsync(
                album, _accountDataHolder.AccountId, _accountDataHolder.AdultConfirmed, AlbumAccessOperation.Assign);
            if (!accessError.Success)
                return accessError;

            await _mediaRepository.AssignAlbumToAccountAsync(accountId, albumId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> AddFilesToAlbumAsync(AddFilesRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(AddFilesToAlbumAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var filesError = _mediaAlbumValidator.ValidateAddFilesRequest(request);
            if (!filesError.Success)
                return filesError;

            var album = await _mediaRepository.GetAlbumAsync(request.AlbumId);

            if (album == null)
                return CommandResult.Fail(ErrorCode.AlbumNotFound, $"Альбом {request.AlbumId} не найден");

            var accessError = await _albumAccessValidator.AssertCanModifyAlbumAsync(
                album, _accountDataHolder.AccountId, _accountDataHolder.AdultConfirmed, AlbumAccessOperation.AddFiles);
            if (!accessError.Success)
                return accessError;

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
                result = await _albumAccessValidator.FilterViewableAlbumsAsync(
                    result, _accountDataHolder.AccountId, _accountDataHolder.AdultConfirmed);
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
            if (result == null)
                return CommandResult<MediaAlbum>.Fail(ErrorCode.AlbumNotFound, "Альбом не найден");

            var accessError = await _albumAccessValidator.AssertCanViewAlbumAsync(
                result, _accountDataHolder.AccountId, _accountDataHolder.AdultConfirmed);
            if (!accessError.Success)
                return CommandResult<MediaAlbum>.Fail(accessError.ErrorCode, accessError.Message);

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
            result = await _albumAccessValidator.FilterViewableAlbumsAsync(
                result, _accountDataHolder.AccountId, _accountDataHolder.AdultConfirmed);

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

            var accessError = await _albumAccessValidator.AssertCanModifyAlbumAsync(
                album, _accountDataHolder.AccountId, _accountDataHolder.AdultConfirmed, AlbumAccessOperation.Delete);
            if (!accessError.Success)
                return accessError;

            var files = await _mediaRepository.GetAlbumFilesAsync(albumId);
            var fileIds = files.Result?.Select(i => i.Id)?.ToList();
            if (fileIds.NullSafeAny())
                await DeleteAbondonedFilesFromFilestorageAsync(fileIds, albumId);

            await _mediaRepository.DeleteAlbumAsync(albumId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> DeleteFilesAsync(List<Guid> fileIds, Guid albumId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteFilesAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (!fileIds.NullSafeAny())
            {
                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return CommandResult.Fail(ErrorCode.AlbumItemNotFound, "Список файлов не должен быть пустым");
            }

            var allFilesExists = await _mediaRepository.CheckFilesExistsAsync(fileIds);

            if (!allFilesExists)
            {
                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return CommandResult.Fail(ErrorCode.AlbumItemNotFound, "Указанные файлы не найдены");
            }

            var album = await _mediaRepository.GetAlbumAsync(albumId);
            if (album == null)
            {
                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return CommandResult.Fail(ErrorCode.AlbumNotFound, "Альбом не найден");
            }

            var accessError = await _albumAccessValidator.AssertCanModifyAlbumAsync(
                album, _accountDataHolder.AccountId, _accountDataHolder.AdultConfirmed, AlbumAccessOperation.Delete);
            if (!accessError.Success)
                return accessError;

            await _mediaRepository.DeleteFilesAsync(fileIds);

            await DeleteAbondonedFilesFromFilestorageAsync(fileIds, albumId);

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
            if (album == null)
                return CommandResult<PagedList<AlbumFile>>.Fail(ErrorCode.AlbumNotFound, "Альбом не найден");

            var accessError = await _albumAccessValidator.AssertCanViewAlbumAsync(
                album, _accountDataHolder.AccountId, _accountDataHolder.AdultConfirmed);
            if (!accessError.Success)
                return CommandResult<PagedList<AlbumFile>>.Fail(accessError.ErrorCode, accessError.Message);

            var result = await _mediaRepository.GetAlbumFilesAsync(albumId);

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

            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

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

        public async Task<CommandResult> DeleteAvatarAsync(Guid fileId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteAvatarAsync)}";
            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var avatar = await _mediaRepository.GetAvatarAsync(fileId);
            if (avatar == null)
            {
                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return CommandResult.Fail(ErrorCode.AlbumItemNotFound, "Файл не найден");
            }

            if (avatar.AccountId != _accountDataHolder.AccountId)
            {
                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return CommandResult.Fail(ErrorCode.AccessError, "Файл принадлежит другмоу аккаунту");
            }

            await _mediaRepository.DeleteAvatarAsync(fileId);

            #region если файл больше нигде не фигурирует то удаляем его физически из файлохранилища            
            var fileInAnotherAlbum = await _mediaRepository.SomeAlbumContainsThisFileAsync(fileId);
            if (!fileInAnotherAlbum)
                await _filestorageClient.DeleteFileAsync(fileId, _accountDataHolder.Token.Value, _accountDataHolder.Jwt);
            #endregion

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        #endregion


        #region organization avatars
        public async Task<CommandResult> SetNewOrganizationAvatarAsync(Guid organizationId, Guid fileId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(SetNewOrganizationAvatarAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            if (!await _organizationsRepository.IsOwnerOrManagerAsync(organizationId, _accountDataHolder.AccountId.Value))
                return CommandResult.Fail(ErrorCode.AccessError, "Изменить аватар организации может только владелец или менеджер");

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

        private async Task DeleteAbondonedFilesFromFilestorageAsync(List<Guid>? fileIds, Guid albumId)
        {
            // проверяем что файлы в удаляемом альбоме не прикреплены к другим альбомам. Если не прикреплены, то удаляем их физически из файлохранилища
            
            if (fileIds.NullSafeAny())
            {
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
        }
    }
}
