using System.Diagnostics;
using EList.Common.CorrelationId;
using EList.Common.Logger;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.Models.Conversations;
using EList.Models.Enums;
using EList.Models.EventsRating;
using EList.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NLog;
using Org.BouncyCastle.Asn1.Ocsp;
using TM.Schedule.API.Attributes;

namespace EList.Api.Controllers
{
    /// <summary>
    /// Контроллер для чатов и сообщений
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [LoggerHandlerWebApiFilter]
    public class ConversationsController : ControllerBase
    {
        #region logger
        private static readonly NLog.ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly ILoggerWrapper logger = new NLogLoggerWrapper(log);
        private const string LOGGER_NAME = "EList.Api.Controllers.ConversationsController.";
        #endregion

        private readonly ICorrelationIdProvider _correlationIdProvider;
        private readonly IDataConnectionProvider _connectionProvider;
        private readonly IConversationService _conversationsService;

        /// <summary>
        /// Конструктор контроллера чатов и сообщений
        /// </summary>
        /// <param name="correlationIdProvider"></param>
        /// <param name="connectionProvider"></param>
        /// <param name="conversationsService"></param>
        public ConversationsController(ICorrelationIdProvider correlationIdProvider, 
            IDataConnectionProvider connectionProvider, 
            IConversationService conversationsService)
        {
            _correlationIdProvider = correlationIdProvider;
            _connectionProvider = connectionProvider;
            _conversationsService = conversationsService;
        }
        
        /// <summary>
        /// Создать новый чат
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("create")]
        public async Task<CommandResult<Guid>> CreateConversationAsync(ConversationRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateConversationAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _conversationsService.CreateConversationAsync(request);

                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();
                else
                    await _connectionProvider.CommitTransactionAsync();

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                await _connectionProvider.RollbackTransactionAsync();
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Удалить чат
        /// </summary>
        /// <param name="conversationId"></param>
        /// <returns></returns>
        [HttpDelete("delete/{conversationId}")]
        public async Task<CommandResult> DeleteConversationAsync(Guid conversationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteConversationAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _conversationsService.DeleteConversationAsync(conversationId);

                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();
                else
                    await _connectionProvider.CommitTransactionAsync();

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                await _connectionProvider.RollbackTransactionAsync();
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Чат
        /// </summary>
        /// <param name="conversationId"></param>
        /// <returns></returns>
        [HttpGet("get/{conversationId}")]
        public async Task<CommandResult<Conversation?>> GetConversationAsync(Guid conversationId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetConversationAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _conversationsService.GetConversationAsync(conversationId);

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Обновить чат
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("update")]
        public async Task<CommandResult> UpdateConversationAsync(ConversationRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateConversationAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _conversationsService.UpdateConversationAsync(request);

                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();
                else
                    await _connectionProvider.CommitTransactionAsync();

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                await _connectionProvider.RollbackTransactionAsync();
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Список чатов пользователя
        /// </summary>
        /// <param name="personalOnly">Показывать только личные чаты (не обсуждения из событий)</param>
        /// <returns></returns>
        [HttpGet("byAccount")]
        public async Task<CommandResult<List<Conversation>>> GetAccountConversationsAsync(bool personalOnly = true)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetAccountConversationsAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                //TODO: Добавить в отображение список всех чатов из событий в которых пользователь состоит или которые организует
                var result = await _conversationsService.GetAccountConversationsAsync(personalOnly);

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Список обсуждений в рамках мероприятия
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        [HttpGet("byEvent/{eventId}")]
        public async Task<CommandResult<List<Conversation>>> GetEventConversations(Guid eventId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetEventConversations)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                //TODO: Добавить в отображение список всех чатов из событий в которых пользователь состоит или которые организует
                var result = await _conversationsService.GetEventConversations(eventId);

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Список сообщений в рамках события
        /// </summary>
        /// <param name="conversationId"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet("messages/{conversationId}")]
        public async Task<CommandResult<PagedList<Message>>> GetConversationMessagesAsync(Guid conversationId, int? pageIndex, int? pageSize)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetConversationMessagesAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _conversationsService.GetConversationMessagesAsync(conversationId, pageIndex, pageSize);

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Список ответов на сообщение
        /// </summary>
        /// <param name="messageId"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet("messages/replies/{messageId}")]
        public async Task<CommandResult<PagedList<Message>>> GetMessageRepliesAsync(Guid messageId, int? pageIndex, int? pageSize)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(GetMessageRepliesAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);

                var result = await _conversationsService.GetMessageRepliesAsync(messageId, pageIndex, pageSize);

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Создать сообщение
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("messages/create")]
        public async Task<CommandResult<Guid>> CreateMessageAsync(MessageRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(CreateMessageAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _conversationsService.CreateMessageAsync(request);

                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();
                else
                    await _connectionProvider.CommitTransactionAsync();

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                await _connectionProvider.RollbackTransactionAsync();
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Редактировать сообщение
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("messages/update")]
        public async Task<CommandResult> UpdateMessageAsync(MessageRequest request)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(UpdateMessageAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _conversationsService.UpdateMessageAsync(request);

                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();
                else
                    await _connectionProvider.CommitTransactionAsync();

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                await _connectionProvider.RollbackTransactionAsync();
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }

        /// <summary>
        /// Удалить сообщение
        /// </summary>
        /// <param name="messageId"></param>
        /// <returns></returns>
        [HttpDelete("messages/{messageId}")]
        public async Task<CommandResult> DeleteMessageAsync(Guid messageId)
        {
            var correlationId = _correlationIdProvider.Get();
            var execTime = Stopwatch.StartNew();
            var methodName = $"{LOGGER_NAME}{nameof(DeleteMessageAsync)}";

            try
            {
                logger.Debug(correlationId, null, methodName, $"Method started", null);
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _conversationsService.DeleteMessageAsync(messageId);

                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();
                else
                    await _connectionProvider.CommitTransactionAsync();

                logger.Debug(correlationId, null, methodName, $"Method finished", null, execTime.Elapsed);
                return result;
            }
            catch (Exception ex)
            {
                await _connectionProvider.RollbackTransactionAsync();
                ExceptionLogger.LogException(logger, correlationId, methodName, "Method failed", execTime.Elapsed, ex);
                throw;
            }
        }
    }
}
