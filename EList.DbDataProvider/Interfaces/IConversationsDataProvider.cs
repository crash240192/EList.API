using EList.DbDataProvider.Models;

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
        Task<ListResponse<MessageDto>> GetMessageRepliesAsync(Guid messageId, int? pageIndex, int? pageSize);

        Task<MessageDto> GetMessageAsync(Guid messageId);
        Task<Guid> CreateMessageAsync(MessageDto message);
        Task UpdateMessageAsync(MessageDto message);
        Task DeleteMessageAsync(Guid messageId);
    }
}
