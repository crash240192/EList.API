using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;

namespace EList.DbDataProvider.Interfaces
{
    public interface IConversationsDataProvider
    {
        Task<Guid> CreateConversationAsync(ConversationDto conversation);
        Task DeleteConversationAsync(Guid conversationId);
        Task<ConversationDto?> GetConversationAsync(Guid conversationId);
        Task UpdateConversationAsync(ConversationDto conversation);

        Task<List<ConversationDto>> GetAccountConversationsAsync(Guid accountId, bool personalOnly);
        Task<List<ConversationDto>> GetEventConversations(Guid eventId);

        Task<ListResponse<MessageDto>> GetConversationMessagesAsync(Guid conversationId, int? pageIndex, int? pageSize);
        /// <summary>Корневые сообщения диалога (без ReplyTo).</summary>
        Task<ListResponse<MessageDto>> GetConversationRootMessagesAsync(Guid conversationId, int? pageIndex, int? pageSize);
        Task<ListResponse<MessageDto>> GetMessageRepliesAsync(Guid messageId, int? pageIndex, int? pageSize);

        Task<MessageDto> GetMessageAsync(Guid messageId);
        Task<Guid> CreateMessageAsync(MessageDto message);
        Task UpdateMessageAsync(MessageDto message);
        Task DeleteMessageAsync(Guid messageId);
        Task AnonymizeAccountMessagesAsync(Guid accountId);
        Task<List<Guid>> GetConversationAuthorAccountIdsAsync(Guid conversationId);

        Task<MessageVoteStatsDto> SetMessageVoteAsync(Guid messageId, Guid accountId, MessageVoteValue value);
        Task<MessageVoteStatsDto> RemoveMessageVoteAsync(Guid messageId, Guid accountId);
        Task<Dictionary<Guid, MessageVoteStatsDto>> GetMessageVoteStatsAsync(IReadOnlyCollection<Guid> messageIds, Guid? currentAccountId);
    }
}
