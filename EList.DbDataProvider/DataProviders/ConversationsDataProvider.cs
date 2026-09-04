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
            conversation.CreateDate = DateTimeOffset.Now;
            conversation.UpdateDate = DateTimeOffset.Now;
            var result = (Guid)await _connection.InsertWithIdentityAsync(conversation);
            return result;
        }

        public async Task<MessageDto> GetMessageAsync(Guid messageId)
        {
            var message = await _connection.Messages.FirstOrDefaultAsync(i => i.Id == messageId);
            return message;
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

        public async Task<List<ConversationDto>> GetAccountConversationsAsync(Guid accountId, bool personalOnly)
        {
            var request = _connection.Messages
                .LoadWith(i => i.Conversation)
                .Where(i => i.AccountId == accountId)
                .Select(i => i.Conversation)
                .DistinctBy(i => i.Id);

            if (personalOnly)
                request = request.Where(i => i.EventId == null);

            request = request.OrderBy(i => i.CreateDate);

            var conversations = await request.ToListAsync();
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
                .LoadWith(i => i.Account)
                .ThenLoad(i => i.Avatars)
                .Where(i => i.ConversationId == conversationId)
                .OrderBy(i => i.CreateDate);
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
                .LoadWith(i => i.Account)
                .ThenLoad(i => i.Avatars)
                .Where(i => i.ReplyTo == messageId)
                .OrderBy(i => i.CreateDate);
            var count = await query.CountAsync();

            var result = await query.ToPagedQuery(pageIndex, pageSize).ToListAsync();

            return new ListResponse<MessageDto>(count, result);
        }

        public async Task UpdateConversationAsync(ConversationDto conversation)
        {
            await _connection.Conversations.Where(i => i.Id == conversation.Id)
                .Set(i => i.EventId, conversation.EventId)
                .Set(i => i.Name, conversation.Name)
                .Set(i => i.ParticipantsOnlyVisible, conversation.ParticipantsOnlyVisible)
                .Set(i => i.ParticipantsReadonly, conversation.ParticipantsReadonly)
                .Set(i => i.UpdateDate, DateTimeOffset.Now)
                .UpdateAsync();
        }


        public async Task<Guid> CreateMessageAsync(MessageDto message)
        {
            message.CreateDate = DateTimeOffset.Now.ToUniversalTime();
            message.UpdateDate = DateTimeOffset.Now.ToUniversalTime();
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
                .Set(i => i.MessageText, message.MessageText)
                .Set(i => i.ReplyTo, message.ReplyTo)
                .Set(i => i.UpdateDate, DateTimeOffset.Now.ToUniversalTime())
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

        public async Task<List<Guid>> GetConversationAuthorAccountIdsAsync(Guid conversationId)
        {
            return await _connection.Messages
                .Where(i => i.ConversationId == conversationId && i.AccountId != null)
                .Select(i => i.AccountId!.Value)
                .Distinct()
                .ToListAsync();
        }
    }
}
