using System.Diagnostics;
using EList.Common.CorrelationId;
using EList.Common.Extensions;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.FilestorageClient;
using EList.Models.Conversations;
using EList.Models.Enums;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using EList.Validators.Interfaces;
using NLog;

namespace EList.Services.Impl
{
    public class ConversationService : IConversationService
    {
        public const int MaxFilesPerMessage = 10;
        public const string DiscussionPhotosAlbumName = "Фото из обсуждений";

        #region logger
        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Services.Impl.ConversationService.";
        #endregion

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IConversationRepository _conversationsRepository;
        private readonly IEventOrganizatorsRepository _eventOrganizatorsRepository;
        private readonly IParticipationsRepository _participationsRepository;
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly INotificationsService _notificationsService;
        private readonly IModerationPenaltiesService _moderationPenaltiesService;
        private readonly IMediaRepository _mediaRepository;
        private readonly IFilestorageClient _filestorageClient;
        private readonly IMediaService _mediaService;
        private readonly IEventAccessValidator _eventAccessValidator;

        public ConversationService(ICorrelationIdProvider correlationIdProvider,
            IConversationRepository conversationsRepository,
            IEventOrganizatorsRepository eventOrganizatorsRepository,
            IParticipationsRepository participationsRepository,
            IAccountDataHolder accountDataHolder,
            INotificationsService notificationsService,
            IModerationPenaltiesService moderationPenaltiesService,
            IMediaRepository mediaRepository,
            IFilestorageClient filestorageClient,
            IMediaService mediaService,
            IEventAccessValidator eventAccessValidator)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _conversationsRepository = conversationsRepository ?? throw new ArgumentNullException(nameof(conversationsRepository));
            _eventOrganizatorsRepository = eventOrganizatorsRepository ?? throw new ArgumentNullException(nameof(eventOrganizatorsRepository));
            _participationsRepository = participationsRepository ?? throw new ArgumentNullException(nameof(participationsRepository));
            _notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
            _moderationPenaltiesService = moderationPenaltiesService ?? throw new ArgumentNullException(nameof(moderationPenaltiesService));
            _accountDataHolder = accountDataHolder;
            _mediaRepository = mediaRepository ?? throw new ArgumentNullException(nameof(mediaRepository));
            _filestorageClient = filestorageClient ?? throw new ArgumentNullException(nameof(filestorageClient));
            _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
            _eventAccessValidator = eventAccessValidator ?? throw new ArgumentNullException(nameof(eventAccessValidator));
        }

        public async Task<CommandResult<Guid>> CreateConversationAsync(ConversationRequest conversation)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateConversationAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (conversation.EventId != null)
            {
                var isAdmin = await IsEventAdminAsync(conversation.EventId.Value);
                if (!isAdmin)
                    return CommandResult<Guid>.Fail(ErrorCode.AccessError, "Создавать диалоги мероприятия может только организатор");
            }

            var result = await _conversationsRepository.CreateConversationAsync(conversation);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid>(result);
        }

        public async Task<CommandResult<Guid>> CreateMessageAsync(MessageRequest message)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateMessageAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var conversation = await _conversationsRepository.GetConversationAsync(message.ConversationId);
            if (conversation == null)
                return CommandResult<Guid>.Fail(ErrorCode.IsNullOrEmpty, "Диалог не найден");

            var writeAccess = await EnsureCanWriteAsync(conversation);
            if (writeAccess != null)
                return CommandResult<Guid>.Fail(writeAccess.ErrorCode, writeAccess.Message);

            var contentError = ValidateMessageContent(message);
            if (contentError != null)
                return CommandResult<Guid>.Fail(contentError.ErrorCode, contentError.Message);

            var fileIds = NormalizeFileIds(message.FileIds);
            if (fileIds.Count > 0 && conversation.EventId == null)
                return CommandResult<Guid>.Fail(ErrorCode.InvalidValue, "Вложения доступны только в обсуждениях мероприятий");

            message.AccountId ??= _accountDataHolder.AccountId;
            message.MessageText ??= string.Empty;
            message.FileIds = fileIds;

            var result = await _conversationsRepository.CreateMessageAsync(message);

            if (fileIds.Count > 0)
            {
                var attachError = await AttachFilesToMessageAsync(
                    conversation.EventId!.Value, result, fileIds, previousFileIds: null);
                if (attachError != null)
                {
                    await _conversationsRepository.DeleteMessageAsync(result);
                    return CommandResult<Guid>.Fail(attachError.ErrorCode, attachError.Message);
                }
            }

            if (message.ReplyTo != null)
                await _notificationsService.NotifyCommentRepliedAsync(conversation.EventId, message.ReplyTo.Value, result);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid>(result);
        }

        public async Task<CommandResult> DeleteConversationAsync(Guid conversationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteConversationAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var conversation = await _conversationsRepository.GetConversationAsync(conversationId);
            if (conversation == null)
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Диалог не найден");

            if (conversation.EventId != null)
            {
                var isAdmin = await IsEventAdminAsync(conversation.EventId.Value);
                if (!isAdmin)
                    return CommandResult.Fail(ErrorCode.AccessError, "Удалять диалоги мероприятия может только организатор");
            }

            var conversationFileIds = await _conversationsRepository.GetConversationMessageFileIdsAsync(conversationId);
            await _conversationsRepository.DeleteConversationAsync(conversationId);

            if (conversation.EventId != null && conversationFileIds.Count > 0)
                await DetachFilesFromDiscussionAlbumAsync(conversation.EventId.Value, conversationFileIds, exceptMessageId: null);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> DeleteMessageAsync(Guid messageId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteMessageAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var existingMessage = await _conversationsRepository.GetMessageAsync(messageId);
            if (existingMessage == null)
                return CommandResult.Fail(ErrorCode.MessageNotFound, "Сообщение не найдено");

            var conversation = await _conversationsRepository.GetConversationAsync(existingMessage.ConversationId);
            if (conversation != null)
            {
                var writeAccess = await EnsureCanWriteAsync(conversation);
                if (writeAccess != null)
                    return writeAccess;
            }

            if (existingMessage.AccountId != _accountDataHolder.AccountId)
                return CommandResult.Fail(ErrorCode.AccessError, "Нельзя удалять чужие сообщения");

            if (existingMessage.Replied)
                return CommandResult.Fail(ErrorCode.MessageReplied, "Нельзя удалить сообщение на которое уже ответили");

            var fileIds = existingMessage.FileIds ?? new List<Guid>();
            await _conversationsRepository.DeleteMessageAsync(messageId);

            if (fileIds.Count > 0 && conversation?.EventId != null)
                await DetachFilesFromDiscussionAlbumAsync(conversation.EventId.Value, fileIds, exceptMessageId: null);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<List<Conversation>>> GetAccountConversationsAsync(bool personalOnly = true)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAccountConversationsAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (_accountDataHolder.AccountId == null)
                return CommandResult<List<Conversation>>.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

            var result = await _conversationsRepository.GetAccountConversationsAsync(_accountDataHolder.AccountId.Value, personalOnly);
            var visible = new List<Conversation>();
            foreach (var conversation in result)
            {
                var access = await AssertCanViewConversationAsync(conversation);
                if (access.Success)
                    visible.Add(conversation);
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<Conversation>>(visible);
        }

        public async Task<CommandResult<Conversation?>> GetConversationAsync(Guid conversationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetConversationAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _conversationsRepository.GetConversationAsync(conversationId);
            if (result == null)
                return new CommandResult<Conversation?>(null);

            var access = await AssertCanViewConversationAsync(result);
            if (!access.Success)
                return CommandResult<Conversation?>.Fail(access.ErrorCode, access.Message);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Conversation?>(result);
        }

        public async Task<CommandResult<PagedList<Message>>> GetConversationMessagesAsync(Guid conversationId, int? pageIndex, int? pageSize)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetConversationMessagesAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var conversation = await _conversationsRepository.GetConversationAsync(conversationId);
            if (conversation == null)
                return CommandResult<PagedList<Message>>.Fail(ErrorCode.IsNullOrEmpty, "Диалог не найден");

            var access = await AssertCanViewConversationAsync(conversation);
            if (!access.Success)
                return CommandResult<PagedList<Message>>.Fail(access.ErrorCode, access.Message);

            var result = await _conversationsRepository.GetConversationMessagesAsync(conversationId, pageIndex, pageSize);
            await _conversationsRepository.ApplyMessageVoteStatsAsync(result.Result, _accountDataHolder.AccountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<Message>>(result);
        }

        public async Task<CommandResult<PagedList<Message>>> GetConversationRootMessagesAsync(Guid conversationId, int? pageIndex, int? pageSize)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetConversationRootMessagesAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var conversation = await _conversationsRepository.GetConversationAsync(conversationId);
            if (conversation == null)
                return CommandResult<PagedList<Message>>.Fail(ErrorCode.IsNullOrEmpty, "Диалог не найден");

            var access = await AssertCanViewConversationAsync(conversation);
            if (!access.Success)
                return CommandResult<PagedList<Message>>.Fail(access.ErrorCode, access.Message);

            var result = await _conversationsRepository.GetConversationRootMessagesAsync(conversationId, pageIndex, pageSize);
            await _conversationsRepository.ApplyMessageVoteStatsAsync(result.Result, _accountDataHolder.AccountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<Message>>(result);
        }

        public async Task<CommandResult<List<Conversation>>> GetEventConversations(Guid eventId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetEventConversations)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var eventAccess = await _eventAccessValidator.AssertCanViewEventAsync(
                eventId, _accountDataHolder.AccountId, _accountDataHolder.AdultConfirmed);
            if (!eventAccess.Success)
                return CommandResult<List<Conversation>>.Fail(eventAccess.ErrorCode, eventAccess.Message);

            var result = await _conversationsRepository.GetEventConversations(eventId);
            var isAdmin = await IsEventAdminAsync(eventId);
            var isParticipant = isAdmin || await IsEventParticipantAsync(eventId);

            var visible = result
                .Where(c => isAdmin || !c.ParticipantsOnlyVisible || isParticipant)
                .ToList();

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<Conversation>>(visible);
        }

        public async Task<CommandResult<PagedList<Message>>> GetMessageRepliesAsync(Guid messageId, int? pageIndex, int? pageSize)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetMessageRepliesAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var message = await _conversationsRepository.GetMessageAsync(messageId);
            if (message == null)
                return CommandResult<PagedList<Message>>.Fail(ErrorCode.MessageNotFound, "Сообщение не найдено");

            var conversation = await _conversationsRepository.GetConversationAsync(message.ConversationId);
            if (conversation != null)
            {
                var access = await AssertCanViewConversationAsync(conversation);
                if (!access.Success)
                    return CommandResult<PagedList<Message>>.Fail(access.ErrorCode, access.Message);
            }

            var result = await _conversationsRepository.GetMessageRepliesAsync(messageId, pageIndex, pageSize);
            await _conversationsRepository.ApplyMessageVoteStatsAsync(result.Result, _accountDataHolder.AccountId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<Message>>(result);
        }

        public async Task<CommandResult<MessageLocation>> GetMessageLocationAsync(
            Guid messageId,
            int? rootPageSize = null,
            int? siblingPageSize = null)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetMessageLocationAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var location = await _conversationsRepository.GetMessageLocationAsync(
                messageId,
                rootPageSize ?? 10,
                siblingPageSize ?? 5);
            if (location == null)
                return CommandResult<MessageLocation>.Fail(ErrorCode.MessageNotFound, "Сообщение не найдено");

            var conversation = await _conversationsRepository.GetConversationAsync(location.ConversationId);
            if (conversation != null)
            {
                var access = await AssertCanViewConversationAsync(conversation);
                if (!access.Success)
                    return CommandResult<MessageLocation>.Fail(access.ErrorCode, access.Message);
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<MessageLocation>(location);
        }

        public async Task<CommandResult> UpdateConversationAsync(ConversationRequest conversation)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateConversationAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (conversation.Id == null)
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Не указан идентификатор диалога");

            var existing = await _conversationsRepository.GetConversationAsync(conversation.Id.Value);
            if (existing == null)
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Диалог не найден");

            var eventId = conversation.EventId ?? existing.EventId;
            if (eventId != null)
            {
                var isAdmin = await IsEventAdminAsync(eventId.Value);
                if (!isAdmin)
                    return CommandResult.Fail(ErrorCode.AccessError, "Редактировать диалоги мероприятия может только организатор");
            }

            await _conversationsRepository.UpdateConversationAsync(conversation);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult> UpdateMessageAsync(MessageRequest message)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateMessageAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            if (message.Id == null)
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, $"Не указан идентификатор сообщения");

            var existingMessage = await _conversationsRepository.GetMessageAsync(message.Id.Value);
            if (existingMessage == null)
                return CommandResult.Fail(ErrorCode.MessageNotFound, $"Сообщение с id='{message.Id}' не найдено");

            var conversation = await _conversationsRepository.GetConversationAsync(existingMessage.ConversationId);
            if (conversation != null)
            {
                var writeAccess = await EnsureCanWriteAsync(conversation);
                if (writeAccess != null)
                    return writeAccess;
            }

            if (existingMessage.AccountId != _accountDataHolder.AccountId)
                return CommandResult.Fail(ErrorCode.AccessError, $"Нельзя редактировать сообщения другого пользователя");

            // Если FileIds не переданы — оставляем прежние вложения.
            if (message.FileIds == null)
                message.FileIds = existingMessage.FileIds;

            var contentError = ValidateMessageContent(message);
            if (contentError != null)
                return contentError;

            var fileIds = NormalizeFileIds(message.FileIds);
            if (fileIds.Count > 0 && conversation?.EventId == null)
                return CommandResult.Fail(ErrorCode.InvalidValue, "Вложения доступны только в обсуждениях мероприятий");

            message.MessageText ??= string.Empty;
            message.FileIds = fileIds;
            message.ConversationId = existingMessage.ConversationId;

            await _conversationsRepository.UpdateMessageAsync(message);

            if (conversation?.EventId != null)
            {
                var attachError = await AttachFilesToMessageAsync(
                    conversation.EventId.Value,
                    message.Id.Value,
                    fileIds,
                    previousFileIds: existingMessage.FileIds);
                if (attachError != null)
                    return attachError;
            }
            else
            {
                await _conversationsRepository.SetMessageFilesAsync(message.Id.Value, fileIds);
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }

        public async Task<CommandResult<MessageVoteResult>> LikeMessageAsync(Guid messageId)
        {
            return await SetMessageVoteAsync(messageId, MessageVoteValue.Like);
        }

        public async Task<CommandResult<MessageVoteResult>> DislikeMessageAsync(Guid messageId)
        {
            return await SetMessageVoteAsync(messageId, MessageVoteValue.Dislike);
        }

        public async Task<CommandResult<MessageVoteResult>> RemoveMessageVoteAsync(Guid messageId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(RemoveMessageVoteAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var access = await EnsureCanVoteAsync(messageId);
            if (access.Error != null)
                return CommandResult<MessageVoteResult>.Fail(access.Error.ErrorCode, access.Error.Message);

            var result = await _conversationsRepository.RemoveMessageVoteAsync(messageId, _accountDataHolder.AccountId.Value);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<MessageVoteResult>(result);
        }

        public async Task CleanupMessageMediaAsync(Guid messageId, Guid? eventId)
        {
            var fileIds = await _conversationsRepository.GetMessageFileIdsAsync(messageId);
            if (!fileIds.NullSafeAny() || eventId == null)
                return;

            await DetachFilesFromDiscussionAlbumAsync(eventId.Value, fileIds, exceptMessageId: messageId);
        }

        private async Task<CommandResult<MessageVoteResult>> SetMessageVoteAsync(Guid messageId, MessageVoteValue value)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}SetMessageVoteAsync";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var access = await EnsureCanVoteAsync(messageId);
            if (access.Error != null)
                return CommandResult<MessageVoteResult>.Fail(access.Error.ErrorCode, access.Error.Message);

            var previousVote = access.Message.CurrentUserVote;
            var result = await _conversationsRepository.SetMessageVoteAsync(messageId, _accountDataHolder.AccountId.Value, value);

            if (value == MessageVoteValue.Like
                && result.CurrentUserVote == MessageVoteValue.Like
                && previousVote != MessageVoteValue.Like)
            {
                await _notificationsService.NotifyCommentLikedAsync(
                    access.Conversation.EventId,
                    messageId,
                    result.LikesCount);
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<MessageVoteResult>(result);
        }

        private static CommandResult? ValidateMessageContent(MessageRequest message)
        {
            var text = message.MessageText?.Trim() ?? string.Empty;
            var fileCount = NormalizeFileIds(message.FileIds).Count;

            if (string.IsNullOrEmpty(text) && fileCount == 0)
                return CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Сообщение должно содержать текст или вложения");

            if (fileCount > MaxFilesPerMessage)
                return CommandResult.Fail(ErrorCode.InvalidValue, $"Не больше {MaxFilesPerMessage} файлов на сообщение");

            message.MessageText = text;
            return null;
        }

        private static List<Guid> NormalizeFileIds(IEnumerable<Guid>? fileIds)
        {
            if (fileIds == null)
                return new List<Guid>();

            return fileIds.Where(id => id != Guid.Empty).Distinct().ToList();
        }

        private async Task<CommandResult?> AttachFilesToMessageAsync(
            Guid eventId,
            Guid messageId,
            List<Guid> fileIds,
            List<Guid>? previousFileIds)
        {
            var previous = previousFileIds ?? new List<Guid>();
            var previousSet = new HashSet<Guid>(previous);
            var nextSet = new HashSet<Guid>(fileIds);

            var toAdd = fileIds.Where(id => !previousSet.Contains(id)).ToList();
            var toRemove = previous.Where(id => !nextSet.Contains(id)).ToList();

            if (toAdd.Count > 0 || fileIds.Count > 0)
            {
                if (_accountDataHolder.AccountId == null)
                    return CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться");

                var albumId = await _mediaRepository.EnsureEventSystemAlbumAsync(
                    eventId,
                    (short)EventAlbumSystemKind.DiscussionPhotos,
                    _accountDataHolder.AccountId.Value,
                    DiscussionPhotosAlbumName);

                if (toAdd.Count > 0)
                {
                    await _mediaRepository.AddFilesToAlbumAsync(albumId, toAdd);
                    await _mediaService.SyncAlbumVisibilityAsync(albumId);
                }
            }

            await _conversationsRepository.SetMessageFilesAsync(messageId, fileIds);

            if (toRemove.Count > 0)
                await DetachFilesFromDiscussionAlbumAsync(eventId, toRemove, exceptMessageId: messageId);

            return null;
        }

        private async Task DetachFilesFromDiscussionAlbumAsync(
            Guid eventId,
            IReadOnlyList<Guid> fileIds,
            Guid? exceptMessageId)
        {
            if (!fileIds.NullSafeAny())
                return;

            var albumId = await _mediaRepository.FindEventSystemAlbumIdAsync(
                eventId, (short)EventAlbumSystemKind.DiscussionPhotos);

            var orphanMessageFiles = await _conversationsRepository.GetOrphanMessageFileIdsAsync(
                fileIds, exceptMessageId);

            if (albumId != null && orphanMessageFiles.Count > 0)
                await _mediaRepository.RemoveFilesFromAlbumAsync(albumId.Value, orphanMessageFiles);

            await DeleteAbandonedFilesFromStorageAsync(orphanMessageFiles, albumId);
        }

        private async Task DeleteAbandonedFilesFromStorageAsync(List<Guid> fileIds, Guid? exceptAlbumId)
        {
            if (!fileIds.NullSafeAny())
                return;

            if (_accountDataHolder.Token == null)
                return;

            List<Guid> candidates = fileIds;
            if (exceptAlbumId != null)
            {
                candidates = await _mediaRepository.GetFilesNotExistsInAnotherAlbumsAsync(fileIds, exceptAlbumId.Value)
                    ?? new List<Guid>();
            }
            else
            {
                var stillInAlbums = new List<Guid>();
                foreach (var fileId in fileIds)
                {
                    if (await _mediaRepository.SomeAlbumContainsThisFileAsync(fileId))
                        stillInAlbums.Add(fileId);
                }
                var stillSet = new HashSet<Guid>(stillInAlbums);
                candidates = fileIds.Where(id => !stillSet.Contains(id)).ToList();
            }

            candidates = await _mediaRepository.FilterUnreferencedFileIdsAsync(candidates);

            foreach (var fileId in candidates)
            {
                try
                {
                    await _filestorageClient.DeleteFileAsync(fileId, _accountDataHolder.Token.Value, _accountDataHolder.Jwt);
                }
                catch
                {
                    // Не блокируем удаление сообщения из‑за сбоя filestorage.
                }
            }
        }

        private async Task<(CommandResult? Error, Message? Message, Conversation? Conversation)> EnsureCanVoteAsync(Guid messageId)
        {
            if (_accountDataHolder.AccountId == null)
                return (CommandResult.Fail(ErrorCode.AccessError, "Необходимо авторизоваться"), null, null);

            var message = await _conversationsRepository.GetMessageAsync(messageId);
            if (message == null)
                return (CommandResult.Fail(ErrorCode.MessageNotFound, "Сообщение не найдено"), null, null);

            if (message.Hidden)
                return (CommandResult.Fail(ErrorCode.AccessError, "Нельзя оценивать скрытое сообщение"), null, null);

            var conversation = await _conversationsRepository.GetConversationAsync(message.ConversationId);
            if (conversation == null)
                return (CommandResult.Fail(ErrorCode.IsNullOrEmpty, "Диалог не найден"), null, null);

            if (conversation.EventId == null)
                return (CommandResult.Fail(ErrorCode.AccessError, "Лайки и дизлайки доступны только для комментариев на страницах мероприятий"), null, null);

            var access = await AssertCanViewConversationAsync(conversation);
            if (!access.Success)
                return (access, null, null);

            var statsMessages = new List<Message> { message };
            await _conversationsRepository.ApplyMessageVoteStatsAsync(statsMessages, _accountDataHolder.AccountId);

            return (null, message, conversation);
        }

        private async Task<bool> IsEventAdminAsync(Guid eventId)
        {
            if (_accountDataHolder.AccountId == null)
                return false;

            return await _eventOrganizatorsRepository.IsAccountEventOrganizatorAsync(eventId, _accountDataHolder.AccountId.Value);
        }

        private async Task<bool> IsEventParticipantAsync(Guid eventId)
        {
            if (_accountDataHolder.AccountId == null)
                return false;

            return await _participationsRepository.IsUserParticipatedAsync(_accountDataHolder.AccountId.Value, eventId);
        }

        /// <summary>
        /// Доступ к диалогу: сначала ACL мероприятия (private / 18+ / ЧС), затем participantsOnlyVisible.
        /// </summary>
        private async Task<CommandResult> AssertCanViewConversationAsync(Conversation conversation)
        {
            if (conversation.EventId != null)
            {
                var eventAccess = await _eventAccessValidator.AssertCanViewEventAsync(
                    conversation.EventId.Value,
                    _accountDataHolder.AccountId,
                    _accountDataHolder.AdultConfirmed);
                if (!eventAccess.Success)
                    return eventAccess;
            }

            if (conversation.EventId == null || !conversation.ParticipantsOnlyVisible)
                return CommandResult.OK;

            if (await IsEventAdminAsync(conversation.EventId.Value))
                return CommandResult.OK;

            if (await IsEventParticipantAsync(conversation.EventId.Value))
                return CommandResult.OK;

            return CommandResult.Fail(ErrorCode.AccessError, "Диалог доступен только участникам мероприятия");
        }

        private async Task<CommandResult?> EnsureCanWriteAsync(Conversation conversation)
        {
            if (conversation.EventId == null)
                return null;

            var viewAccess = await AssertCanViewConversationAsync(conversation);
            if (!viewAccess.Success)
                return viewAccess;

            var isAdmin = await IsEventAdminAsync(conversation.EventId.Value);
            if (isAdmin)
                return null;

            if (conversation.ParticipantsReadonly)
                return CommandResult.Fail(ErrorCode.AccessError, "Участники могут только читать сообщения в этом диалоге");

            if (_accountDataHolder.AccountId != null)
            {
                var messagingBan = await _moderationPenaltiesService.AssertNotRestrictedAsync(
                    _accountDataHolder.AccountId.Value, EList.Models.Enums.ModerationPenaltyType.BanMessaging);
                if (!messagingBan.Success)
                    return CommandResult.Fail(messagingBan.ErrorCode, messagingBan.Message);
            }

            return null;
        }
    }
}
