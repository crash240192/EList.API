using EList.Common.Models;
using EList.Models.Conversations;

namespace EList.Repositories.Interfaces
{
    public interface IConversationRepository
    {
        Task<Guid> CreateConversationAsync(ConversationRequest conversation);
        Task DeleteConversationAsync(Guid conversationId);
        Task<Conversation?> GetConversationAsync(Guid conversationId);
        Task UpdateConversationAsync(ConversationRequest conversation);

        Task<List<Conversation>> GetAccountConversationsAsync(Guid accountId, bool personalOnly);
        Task<List<Conversation>> GetEventConversations(Guid eventId);

        Task<PagedList<Message>> GetConversationMessagesAsync(Guid conversationId, int? pageIndex, int? pageSize);
        Task<PagedList<Message>> GetMessageRepliesAsync(Guid messageId, int? pageIndex, int? pageSize);

        Task<Message> GetMessageAsync(Guid messageId);
        Task<Guid> CreateMessageAsync(MessageRequest message);
        Task<bool> CheckMessageRepliedAsync(Guid id);
        Task UpdateMessageAsync(MessageRequest message);
        Task DeleteMessageAsync(Guid messageId);
    }
}
