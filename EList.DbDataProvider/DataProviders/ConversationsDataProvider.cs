using EList.DbDataProvider.Extensions;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
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
            await _connection.MessageFiles.DeleteAsync(i => i.MessageId == messageId);
            await _connection.Messages.DeleteAsync(i => i.Id == messageId);
        }

        public async Task SetMessageFilesAsync(Guid messageId, IReadOnlyList<Guid> fileIds)
        {
            await _connection.MessageFiles.DeleteAsync(i => i.MessageId == messageId);

            if (fileIds == null || fileIds.Count == 0)
                return;

            var rows = fileIds
                .Distinct()
                .Select((fileId, index) => new MessageFileDto
                {
                    Id = Guid.NewGuid(),
                    MessageId = messageId,
                    FileId = fileId,
                    SortOrder = index
                })
                .ToList();

            await _connection.BulkCopyAsync(rows);
        }

        public async Task<List<Guid>> GetMessageFileIdsAsync(Guid messageId)
        {
            return await _connection.MessageFiles
                .Where(i => i.MessageId == messageId)
                .OrderBy(i => i.SortOrder)
                .Select(i => i.FileId)
                .ToListAsync();
        }

        public async Task<Dictionary<Guid, List<Guid>>> GetMessageFilesMapAsync(IReadOnlyCollection<Guid> messageIds)
        {
            var result = messageIds
                .Distinct()
                .ToDictionary(id => id, _ => new List<Guid>());

            if (result.Count == 0)
                return result;

            var rows = await _connection.MessageFiles
                .Where(i => messageIds.Contains(i.MessageId))
                .OrderBy(i => i.SortOrder)
                .Select(i => new { i.MessageId, i.FileId })
                .ToListAsync();

            foreach (var row in rows)
            {
                if (result.TryGetValue(row.MessageId, out var list))
                    list.Add(row.FileId);
            }

            return result;
        }

        public async Task<List<Guid>> GetOrphanMessageFileIdsAsync(IReadOnlyList<Guid> fileIds, Guid? exceptMessageId)
        {
            if (fileIds == null || fileIds.Count == 0)
                return new List<Guid>();

            var stillReferenced = await _connection.MessageFiles
                .Where(i => fileIds.Contains(i.FileId)
                    && (exceptMessageId == null || i.MessageId != exceptMessageId.Value))
                .Select(i => i.FileId)
                .Distinct()
                .ToListAsync();

            var referenced = new HashSet<Guid>(stillReferenced);
            return fileIds.Where(id => !referenced.Contains(id)).Distinct().ToList();
        }

        public async Task<List<Guid>> GetConversationMessageFileIdsAsync(Guid conversationId)
        {
            return await (
                from mf in _connection.MessageFiles
                join m in _connection.Messages on mf.MessageId equals m.Id
                where m.ConversationId == conversationId
                select mf.FileId
            ).Distinct().ToListAsync();
        }

        public async Task<List<ConversationDto>> GetAccountConversationsAsync(Guid accountId, bool personalOnly)
        {
            // Чаты, в которых пользователь уже писал сообщения.
            var messagedQuery = _connection.Messages
                .LoadWith(i => i.Conversation)
                .Where(i => i.AccountId == accountId)
                .Select(i => i.Conversation);

            if (personalOnly)
                messagedQuery = messagedQuery.Where(i => i.EventId == null);

            var conversations = await messagedQuery.ToListAsync();
            var byId = conversations
                .Where(c => c != null)
                .GroupBy(c => c.Id)
                .ToDictionary(g => g.Key, g => g.First());

            // При personalOnly=false добавляем event-чаты событий,
            // где пользователь участник или организатор (даже без своих сообщений).
            if (!personalOnly)
            {
                var participatedEventIds = await _connection.Participations
                    .Where(p => p.AccountId == accountId)
                    .Select(p => p.EventId)
                    .ToListAsync();

                var directOrgEventIds = await _connection.Organizators
                    .Where(o => o.AccountId == accountId)
                    .Select(o => o.EventId)
                    .ToListAsync();

                var memberOrgIds = await _connection.OrganizationMembers
                    .Where(m => m.AccountId == accountId && m.Active)
                    .Select(m => m.OrganizationId)
                    .ToListAsync();

                var orgMemberEventIds = memberOrgIds.Count == 0
                    ? new List<Guid>()
                    : await _connection.Organizators
                        .Where(o => o.OrganizationId != null && memberOrgIds.Contains(o.OrganizationId.Value))
                        .Select(o => o.EventId)
                        .ToListAsync();

                var eventIds = participatedEventIds
                    .Concat(directOrgEventIds)
                    .Concat(orgMemberEventIds)
                    .Distinct()
                    .ToList();

                if (eventIds.Count > 0)
                {
                    var eventConversations = await _connection.Conversations
                        .Where(c => c.EventId != null && eventIds.Contains(c.EventId.Value))
                        .ToListAsync();

                    foreach (var conversation in eventConversations)
                    {
                        if (!byId.ContainsKey(conversation.Id))
                            byId[conversation.Id] = conversation;
                    }
                }
            }

            return byId.Values.OrderBy(c => c.CreateDate).ToList();
        }

        public async Task AnonymizeAccountMessagesAsync(Guid accountId)
        {
            await _connection.Messages
                .Where(m => m.AccountId == accountId)
                .Set(m => m.MessageText, "[сообщение удалено]")
                .Set(m => m.Hidden, true)
                .Set(m => m.HiddenAt, DateTimeOffset.UtcNow)
                .Set(m => m.UpdateDate, DateTimeOffset.UtcNow)
                .UpdateAsync();
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

        public async Task<ListResponse<MessageDto>> GetConversationRootMessagesAsync(Guid conversationId, int? pageIndex, int? pageSize)
        {
            var query = _connection.Messages
                .LoadWith(i => i.Account)
                .ThenLoad(i => i.PersonInfo)
                .LoadWith(i => i.Account)
                .ThenLoad(i => i.Avatars)
                .Where(i => i.ConversationId == conversationId && i.ReplyTo == null)
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

        public async Task<int> GetMessagePageIndexAsync(Guid conversationId, Guid? parentId, Guid messageId, int pageSize)
        {
            if (pageSize <= 0) pageSize = 10;

            var target = await _connection.Messages.FirstOrDefaultAsync(i => i.Id == messageId);
            if (target == null || target.ConversationId != conversationId)
                return 0;

            // Тот же порядок, что у списка: OrderBy(CreateDate)
            var beforeCount = await _connection.Messages
                .Where(i => i.ConversationId == conversationId
                    && i.ReplyTo == parentId
                    && i.CreateDate < target.CreateDate)
                .CountAsync();

            return beforeCount / pageSize;
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

        public async Task<MessageVoteStatsDto> SetMessageVoteAsync(Guid messageId, Guid accountId, MessageVoteValue value)
        {
            var now = DateTimeOffset.UtcNow;
            var existing = await _connection.MessageVotes
                .FirstOrDefaultAsync(i => i.MessageId == messageId && i.AccountId == accountId);

            if (existing == null)
            {
                await _connection.InsertAsync(new MessageVoteDto
                {
                    MessageId = messageId,
                    AccountId = accountId,
                    Value = value,
                    CreateDate = now,
                    UpdateDate = now
                });
            }
            else if (existing.Value == value)
            {
                await _connection.MessageVotes.DeleteAsync(i => i.Id == existing.Id);
            }
            else
            {
                await _connection.MessageVotes
                    .Where(i => i.Id == existing.Id)
                    .Set(i => i.Value, value)
                    .Set(i => i.UpdateDate, now)
                    .UpdateAsync();
            }

            var stats = await GetMessageVoteStatsAsync(new[] { messageId }, accountId);
            return stats[messageId];
        }

        public async Task<MessageVoteStatsDto> RemoveMessageVoteAsync(Guid messageId, Guid accountId)
        {
            await _connection.MessageVotes.DeleteAsync(i => i.MessageId == messageId && i.AccountId == accountId);
            var stats = await GetMessageVoteStatsAsync(new[] { messageId }, accountId);
            return stats[messageId];
        }

        public async Task<Dictionary<Guid, MessageVoteStatsDto>> GetMessageVoteStatsAsync(
            IReadOnlyCollection<Guid> messageIds,
            Guid? currentAccountId)
        {
            var result = messageIds
                .Distinct()
                .ToDictionary(id => id, id => new MessageVoteStatsDto { MessageId = id });

            if (result.Count == 0)
                return result;

            var votes = await _connection.MessageVotes
                .Where(v => messageIds.Contains(v.MessageId))
                .Select(v => new { v.MessageId, v.AccountId, v.Value })
                .ToListAsync();

            foreach (var vote in votes)
            {
                if (!result.TryGetValue(vote.MessageId, out var stats))
                    continue;

                if (vote.Value == MessageVoteValue.Like)
                    stats.LikesCount++;
                else
                    stats.DislikesCount++;

                if (currentAccountId != null && vote.AccountId == currentAccountId)
                    stats.CurrentUserVote = vote.Value;
            }

            return result;
        }
    }
}
