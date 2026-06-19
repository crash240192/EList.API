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
        private readonly IAccountDataHolder _accountDataHolder;
        private readonly INotificationsService _notificationsService;
        public ConversationService(ICorrelationIdProvider correlationIdProvider,
            IConversationRepository conversationsRepository,
            IAccountDataHolder accountDataHolder,
            INotificationsService notificationsService)
        {
            _correlationIdProvider = correlationIdProvider ?? throw new ArgumentNullException(nameof(correlationIdProvider));
            _conversationsRepository = conversationsRepository ?? throw new ArgumentNullException(nameof(conversationsRepository));
            _notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
            _accountDataHolder = accountDataHolder;
        }

        public async Task<CommandResult<Guid>> CreateConversationAsync(ConversationRequest conversation)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateConversationAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            //TODO: Добавить проверку что пользователь может создать диалог в рамках текущего события
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

            //TODO: Добавить проверку что пользователь может писать в указанном чате
            var result = await _conversationsRepository.CreateMessageAsync(message);

            if (message.ReplyTo != null)
            {
                var conversation = await _conversationsRepository.GetConversationAsync(message.ConversationId);
                await _notificationsService.NotifyCommentRepliedsync(conversation?.EventId, message.ReplyTo.Value, result);
            }

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Guid>(result);
        }

        public async Task<CommandResult> DeleteConversationAsync(Guid conversationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteConversationAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            //TODO: Добавить проверку что пользователь может удалить диалог
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

            if (existingMessage.AccountId != _accountDataHolder.AccountId)
                return CommandResult.Fail(ErrorCode.MessageNotFound, "Сообщение не найдено");

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

            var result = await _conversationsRepository.GetAccountConversationsAsync(_accountDataHolder.AccountId, personalOnly);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<Conversation>>(result);
        }

        public async Task<CommandResult<Conversation?>> GetConversationAsync(Guid conversationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetConversationAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            var result = await _conversationsRepository.GetConversationAsync(conversationId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<Conversation?>(result);
        }

        public async Task<CommandResult<PagedList<Message>>> GetConversationMessagesAsync(Guid conversationId, int? pageIndex, int? pageSize)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetConversationMessagesAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            //TODO: Реализовать проверку что пользователь может просматривать сообщения этого чата
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

            //TODO: Реализовать проверку что пользователь может видеть чаты события (белые, черные списки, закрытое и т.д.)
            var result = await _conversationsRepository.GetEventConversations(eventId);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return new CommandResult<List<Conversation>>(result);
        }

        public async Task<CommandResult<PagedList<Message>>> GetMessageRepliesAsync(Guid messageId, int? pageIndex, int? pageSize)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetMessageRepliesAsync)}";

            logger.Debug(correlationId, null, methodName, $"Method started", null);

            //TODO: Реализовать проверку что пользователь может видеть эти сообщения (белые, черные списки, закрытое и т.д.)
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

            //TODO: Реализовать проверку что пользователь редактировать диалог
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

            if (existingMessage.AccountId != _accountDataHolder.AccountId)
                return CommandResult.Fail(ErrorCode.AccessError, $"Нельзя редактировать сообщения другого пользователя");

            await _conversationsRepository.UpdateMessageAsync(message);

            logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
            return CommandResult.OK;
        }
    }
}
