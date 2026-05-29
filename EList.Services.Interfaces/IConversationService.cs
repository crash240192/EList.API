using EList.Common.Models;
using EList.Models.Conversations;

namespace EList.Services.Interfaces
{
    public interface IConversationService
    {
        Task<CommandResult<Guid>> CreateConversationAsync(ConversationRequest conversation);
        Task<CommandResult> DeleteConversationAsync(Guid conversationId);
        Task<CommandResult<Conversation?>> GetConversationAsync(Guid conversationId);
        Task<CommandResult> UpdateConversationAsync(ConversationRequest conversation);

        Task<CommandResult<List<Conversation>>> GetAccountConversationsAsync(bool personalOnly = true);
        Task<CommandResult<List<Conversation>>> GetEventConversations(Guid eventId);

        Task<CommandResult<PagedList<Message>>> GetConversationMessagesAsync(Guid conversationId, int? pageIndex, int? pageSize);
        Task<CommandResult<PagedList<Message>>> GetMessageRepliesAsync(Guid messageId, int? pageIndex, int? pageSize);

        Task<CommandResult<Guid>> CreateMessageAsync(MessageRequest message);
        Task<CommandResult> UpdateMessageAsync(MessageRequest message);
        Task<CommandResult> DeleteMessageAsync(Guid messageId);
    }
}
