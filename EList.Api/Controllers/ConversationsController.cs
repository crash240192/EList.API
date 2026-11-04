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
using Org.BouncyCastle.Asn1.Ocsp;

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
        private readonly IDataConnectionProvider _connectionProvider;
        private readonly IConversationService _conversationsService;

        /// <summary>
        /// Конструктор контроллера чатов и сообщений
        /// </summary>
        /// <param name="correlationIdProvider"></param>
        /// <param name="connectionProvider"></param>
        /// <param name="conversationsService"></param>
        public ConversationsController(IDataConnectionProvider connectionProvider, 
            IConversationService conversationsService)
        {
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
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _conversationsService.CreateConversationAsync(request);

                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
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
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _conversationsService.DeleteConversationAsync(conversationId);

                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
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
            var result = await _conversationsService.GetConversationAsync(conversationId);
            return result;
        }

        /// <summary>
        /// Обновить чат
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("update")]
        public async Task<CommandResult> UpdateConversationAsync(ConversationRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _conversationsService.UpdateConversationAsync(request);

                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
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
            // personalOnly=false → личные + event-чаты событий, где пользователь участник/организатор
            var result = await _conversationsService.GetAccountConversationsAsync(personalOnly);
            return result;
        }

        /// <summary>
        /// Список обсуждений в рамках мероприятия
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        [HttpGet("byEvent/{eventId}")]
        public async Task<CommandResult<List<Conversation>>> GetEventConversations(Guid eventId)
        {
            // byEvent возвращает чаты конкретного события; ACL видимости — в ConversationService.
            var result = await _conversationsService.GetEventConversations(eventId);
            return result;

        }

        /// <summary>
        /// Список сообщений в рамках события
        /// </summary>
        /// <param name="conversationId"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet("messages/byConversationId/{conversationId}")]
        public async Task<CommandResult<PagedList<Message>>> GetConversationMessagesAsync(Guid conversationId, int? pageIndex, int? pageSize)
        {
            var result = await _conversationsService.GetConversationMessagesAsync(conversationId, pageIndex, pageSize);
            return result;

        }

        /// <summary>
        /// Корневые комментарии диалога (ReplyTo IS NULL), с пагинацией
        /// </summary>
        [HttpGet("messages/roots/byConversationId/{conversationId}")]
        public async Task<CommandResult<PagedList<Message>>> GetConversationRootMessagesAsync(Guid conversationId, int? pageIndex, int? pageSize)
        {
            var result = await _conversationsService.GetConversationRootMessagesAsync(conversationId, pageIndex, pageSize);
            return result;

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
            var result = await _conversationsService.GetMessageRepliesAsync(messageId, pageIndex, pageSize);
            return result;

        }

        /// <summary>
        /// Позиция сообщения в дереве (корень, предки, индексы страниц) для deep-link из уведомлений.
        /// </summary>
        [HttpGet("messages/{messageId}/location")]
        public async Task<CommandResult<MessageLocation>> GetMessageLocationAsync(
            Guid messageId,
            int? rootPageSize = null,
            int? siblingPageSize = null)
        {
            var result = await _conversationsService.GetMessageLocationAsync(messageId, rootPageSize, siblingPageSize);
            return result;

        }

        /// <summary>
        /// Создать сообщение
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("messages/create")]
        public async Task<CommandResult<Guid>> CreateMessageAsync(MessageRequest request)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _conversationsService.CreateMessageAsync(request);

                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
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
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _conversationsService.UpdateMessageAsync(request);

                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
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
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _conversationsService.DeleteMessageAsync(messageId);

                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Поставить лайк комментарию на странице мероприятия.
        /// Повторный вызов снимает лайк. Если стоял дизлайк — он заменяется на лайк.
        /// </summary>
        [HttpPost("messages/{messageId}/like")]
        public async Task<CommandResult<MessageVoteResult>> LikeMessageAsync(Guid messageId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _conversationsService.LikeMessageAsync(messageId);

                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Поставить дизлайк комментарию на странице мероприятия.
        /// Повторный вызов снимает дизлайк. Если стоял лайк — он заменяется на дизлайк.
        /// </summary>
        [HttpPost("messages/{messageId}/dislike")]
        public async Task<CommandResult<MessageVoteResult>> DislikeMessageAsync(Guid messageId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _conversationsService.DislikeMessageAsync(messageId);

                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }

        /// <summary>
        /// Снять свою оценку (лайк или дизлайк) с комментария.
        /// </summary>
        [HttpDelete("messages/{messageId}/vote")]
        public async Task<CommandResult<MessageVoteResult>> RemoveMessageVoteAsync(Guid messageId)
        {
            try
            {
                await _connectionProvider.StartNewTransactionAsync();

                var result = await _conversationsService.RemoveMessageVoteAsync(messageId);

                if (!result.Success)
                    await _connectionProvider.RollbackTransactionAsync();

                return result;
            }
            catch
            {
                await _connectionProvider.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
