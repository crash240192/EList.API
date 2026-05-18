using EList.DbDataProvider.Extensions;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.VisualBasic;

namespace EList.DbDataProvider.DataProviders
{
    public class ConversationsDataProvider : DataProviderBase, IConversationsDataProvider
    {
        public ConversationsDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }

        public async Task<Guid> CreateConversationAsync(ConversationDto conversation)
        {
            var result = (Guid)await _connection.InsertWithIdentityAsync(conversation);
            return result;
        }

        public async Task DeleteConversationAsync(Guid conversationId)
        {
            await _connection.Messages.DeleteAsync(i => i.ConversationId == conversationId);
            await _connection.Conversations.DeleteAsync(i => i.Id == conversationId);
        }

        public async Task DeleteMessageAsync(Guid messageId)
        {
            await _connection.Messages.DeleteAsync(i => i.Id == messageId);
        }

        public async Task<List<ConversationDto>> GetAccountConversationsAsync(Guid accountId)
        {
            var conversations = await _connection.Messages
                .LoadWith(i => i.Conversation)
                .Where(i => i.AccountId == accountId)
                .Select(i => i.Conversation)
                .DistinctBy(i => i.Id)
                .ToListAsync();
            return conversations;
        }

        public async Task<ConversationDto?> GetConversationAsync(Guid conversationId)
        {
            var conversation = await _connection.Conversations.FirstOrDefaultAsync(i => i.Id == conversationId);
            return conversation;
        }

        public async Task<ListResponse<MessageDto>> GetConversationMessagesAsync(Guid conversationId, int? pageIndex, int? pageSize)
        {
            var query = _connection.Messages
                .LoadWith(i => i.Account)
                .ThenLoad(i => i.PersonInfo)
                .Where(i => i.ConversationId == conversationId);
            var count = await query.CountAsync();
            
            var result = await query.ToPagedQuery(pageIndex, pageSize).ToListAsync();
            return new ListResponse<MessageDto>(count, result);
        }

        public async Task<List<ConversationDto>> GetEventConversations(Guid eventId)
        {
            var result = await _connection.Conversations.Where(i =>  eventId == i.EventId).ToListAsync();
            return result;
        }

        public async Task<ListResponse<MessageDto>> GetMessageRepliesAsync(Guid messageId, int? pageIndex, int? pageSize)
        {
            var query = _connection.Messages
                .LoadWith(i => i.Account)
                .ThenLoad(i => i.PersonInfo)
                .Where(i => i.ReplyTo == messageId);
            var count = await query.CountAsync();

            var result = await query.ToPagedQuery(pageIndex, pageSize).ToListAsync();

            return new ListResponse<MessageDto>(count, result);
        }

        public async Task UpdateConversationAsync(ConversationDto conversation)
        {
            await _connection.Conversations.Where(i => i.Id == conversation.Id)
                .Set(i => i.EventId, conversation.Id)
                .Set(i => i.Name, conversation.Name)
                .UpdateAsync();
        }


        public async Task<Guid> CreateMessageAsync(MessageDto message)
        {
            var result = (Guid)await _connection.InsertWithIdentityAsync(message);

            if (message.ReplyTo != null)
                await _connection.Messages.Where(i => i.Id == message.ReplyTo)
                        .Set(i => i.Replied, true)
                        .UpdateAsync();

            return result;
        }

        public async Task UpdateMessageAsync(MessageDto message)
        {
            var existingMessage = await _connection.Messages.FirstAsync(i => i.Id == message.Id);
            await _connection.Messages.Where(i => i.Id == message.Id)
                .Set(i => i.Message, message.Message)
                .Set(i => i.ReplyTo, message.ReplyTo)
                .UpdateAsync();

            if (existingMessage.ReplyTo != message.ReplyTo)
            {
                if (existingMessage.ReplyTo != null)
                {
                    var oldMessageRepliesCount = await _connection.Messages.Where(i => i.ReplyTo == existingMessage.ReplyTo && i.Id != existingMessage.Id).CountAsync();
                    if (!(oldMessageRepliesCount > 0)) 
                        await _connection.Messages.Where(i => i.Id == existingMessage.ReplyTo)
                            .Set(i => i.Replied, false)
                            .UpdateAsync();
                }

                if (message.ReplyTo != null)
                {
                    await _connection.Messages.Where(i => i.Id == message.ReplyTo)
                        .Set(i => i.Replied, true)
                        .UpdateAsync();
                }                
            }
        }
    }
}
