using System.Diagnostics;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.Common.Support;
using EList.Models.Conversations;
using EList.Repositories.Interfaces;
using EList.Services.Interfaces;
using NLog;

namespace EList.Services.Impl
{
    public class ConversationService : IConversationService
    {
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

        public ConversationService(ICorrelationIdProvider correlationIdProvider,
            IConversationRepository conversationsRepository,
            IEventOrganizatorsRepository eventOrganizatorsRepository,
            IParticipationsRepository participationsRepository,
            IAccountDataHolder accountDataHolder,
            INotificationsService notificationsService,
            IModerationPenaltiesService moderationPenaltiesService)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _conversationsRepository = conversationsRepository ?? throw new ArgumentNullException(nameof(conversationsRepository));
            _eventOrganizatorsRepository = eventOrganizatorsRepository ?? throw new ArgumentNullException(nameof(eventOrganizatorsRepository));
            _participationsRepository = participationsRepository ?? throw new ArgumentNullException(nameof(participationsRepository));
            _notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
            _moderationPenaltiesService = moderationPenaltiesService ?? throw new ArgumentNullException(nameof(moderationPenaltiesService));
            _accountDataHolder = accountDataHolder;
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

            message.AccountId ??= _accountDataHolder.AccountId;
            var result = await _conversationsRepository.CreateMessageAsync(message);

            if (message.ReplyTo != null)
                await _notificationsService.NotifyCommentRepliedsync(conversation.EventId, message.ReplyTo.Value, result);

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

            await _conversationsRepository.DeleteConversationAsync(conversationId);

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

            await _conversationsRepository.DeleteMessageAsync(messageId);

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
                if (await CanViewConversationAsync(conversation))
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

            if (!await CanViewConversationAsync(result))
                return CommandResult<Conversation?>.Fail(ErrorCode.AccessError, "Диалог доступен только участникам мероприятия");

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

            if (!await CanViewConversationAsync(conversation))
                return CommandResult<PagedList<Message>>.Fail(ErrorCode.AccessError, "Диалог доступен только участникам мероприятия");

            var result = await _conversationsRepository.GetConversationMessagesAsync(conversationId, pageIndex, pageSize);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<Message>>(result);
        }

        public async Task<CommandResult<List<Conversation>>> GetEventConversations(Guid eventId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetEventConversations)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

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
            if (conversation != null && !await CanViewConversationAsync(conversation))
                return CommandResult<PagedList<Message>>.Fail(ErrorCode.AccessError, "Диалог доступен только участникам мероприятия");

            var result = await _conversationsRepository.GetMessageRepliesAsync(messageId, pageIndex, pageSize);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<PagedList<Message>>(result);
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

            await _conversationsRepository.UpdateMessageAsync(message);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
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

        private async Task<bool> CanViewConversationAsync(Conversation conversation)
        {
            if (conversation.EventId == null || !conversation.ParticipantsOnlyVisible)
                return true;

            if (await IsEventAdminAsync(conversation.EventId.Value))
                return true;

            return await IsEventParticipantAsync(conversation.EventId.Value);
        }

        private async Task<CommandResult?> EnsureCanWriteAsync(Conversation conversation)
        {
            if (conversation.EventId == null)
                return null;

            var isAdmin = await IsEventAdminAsync(conversation.EventId.Value);
            if (isAdmin)
                return null;

            if (conversation.ParticipantsOnlyVisible)
            {
                var isParticipant = await IsEventParticipantAsync(conversation.EventId.Value);
                if (!isParticipant)
                    return CommandResult.Fail(ErrorCode.AccessError, "Диалог доступен только участникам мероприятия");
            }

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
