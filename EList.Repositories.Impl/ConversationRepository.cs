using AutoMapper;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.Accounts;
using EList.Models.Conversations;
using EList.Models.Enums;
using EList.Models.Person;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly IConversationsDataProvider _conversationsDataProvider;
        private readonly IMapper _mapper;

        public ConversationRepository(IConversationsDataProvider conversationsDataProvider, IMapper mapper)
        {
            _conversationsDataProvider = conversationsDataProvider;
            _mapper = mapper;
        }

        public async Task<Guid> CreateConversationAsync(ConversationRequest conversation)
        {
            var mappedRequest = _mapper.Map<ConversationDto>(conversation);
            var result = await _conversationsDataProvider.CreateConversationAsync(mappedRequest);
            return result;
        }

        public async Task<List<Conversation>> GetAccountConversationsAsync(Guid accountId, bool personalOnly)
        {
            var dbResult = await _conversationsDataProvider.GetAccountConversationsAsync(accountId, personalOnly);
            var mappedResult = _mapper.Map<List<Conversation>>(dbResult);
            return mappedResult;
        }

        public async Task<List<Conversation>> GetEventConversations(Guid eventId)
        {
            var dbResult = await _conversationsDataProvider.GetEventConversations(eventId);
            var mappedResult = _mapper.Map<List<Conversation>>(dbResult);
            return mappedResult;
        }

        public async Task<Conversation?> GetConversationAsync(Guid conversationId)
        {
            var dbResult = await _conversationsDataProvider.GetConversationAsync(conversationId);
            var mappedResult = _mapper.Map<Conversation?>(dbResult);
            return mappedResult;
        }

        public async Task UpdateConversationAsync(ConversationRequest conversation)
        {
            var mappedRequest = _mapper.Map<ConversationDto>(conversation);
            await _conversationsDataProvider.UpdateConversationAsync(mappedRequest);
        }

        public async Task DeleteConversationAsync(Guid conversationId)
        {
            await _conversationsDataProvider.DeleteConversationAsync(conversationId);
        }




        public async Task<Guid> CreateMessageAsync(MessageRequest message)
        {
            var mappedRequest = _mapper.Map<MessageDto>(message);
            var result = await _conversationsDataProvider.CreateMessageAsync(mappedRequest);
            return result;
        }

        public async Task DeleteMessageAsync(Guid messageId)
        {
            await _conversationsDataProvider.DeleteMessageAsync(messageId);
        }

        public async Task AnonymizeAccountMessagesAsync(Guid accountId)
        {
            await _conversationsDataProvider.AnonymizeAccountMessagesAsync(accountId);
        }

        public async Task<Message> GetMessageAsync(Guid messageId)
        {
            var dbResult = await _conversationsDataProvider.GetMessageAsync(messageId);
            var mappedResult = _mapper.Map<Message>(dbResult);
            return mappedResult;
        }

        public async Task<PagedList<Message>> GetConversationMessagesAsync(Guid conversationId, int? pageIndex, int? pageSize)
        {
            var dbResult = await _conversationsDataProvider.GetConversationMessagesAsync(conversationId, pageIndex, pageSize);
            var mappedResult = dbResult.Items?.Select(i =>
            {
                var message = _mapper.Map<Message>(i);
                message.Account = _mapper.Map<AccountPublicData>(i.Account);
                message.PersonInfo = _mapper.Map<PersonInfo>(i.Account.PersonInfo);
                return message;
            })?.ToList();
            return new PagedList<Message>(dbResult.TotalCount, mappedResult, pageIndex ?? 0, pageSize ?? dbResult.TotalCount);
        }

        public async Task<PagedList<Message>> GetConversationRootMessagesAsync(Guid conversationId, int? pageIndex, int? pageSize)
        {
            var dbResult = await _conversationsDataProvider.GetConversationRootMessagesAsync(conversationId, pageIndex, pageSize);
            var mappedResult = dbResult.Items?.Select(i =>
            {
                var message = _mapper.Map<Message>(i);
                message.Account = _mapper.Map<AccountPublicData>(i.Account);
                message.PersonInfo = _mapper.Map<PersonInfo>(i.Account.PersonInfo);
                return message;
            })?.ToList();
            return new PagedList<Message>(dbResult.TotalCount, mappedResult, pageIndex ?? 0, pageSize ?? dbResult.TotalCount);
        }

        public async Task<PagedList<Message>> GetMessageRepliesAsync(Guid messageId, int? pageIndex, int? pageSize)
        {
            var dbResult = await _conversationsDataProvider.GetMessageRepliesAsync(messageId, pageIndex, pageSize);
            var mappedResult = dbResult.Items?.Select(i =>
            {
                var message = _mapper.Map<Message>(i);
                message.Account = _mapper.Map<AccountPublicData>(i.Account);
                message.PersonInfo = _mapper.Map<PersonInfo>(i.Account.PersonInfo);
                return message;
            })?.ToList();
            return new PagedList<Message>(dbResult.TotalCount, mappedResult, pageIndex ?? 0, pageSize ?? dbResult.TotalCount);
        }

        public async Task<MessageLocation?> GetMessageLocationAsync(Guid messageId, int rootPageSize, int siblingPageSize)
        {
            if (rootPageSize <= 0) rootPageSize = 10;
            if (siblingPageSize <= 0) siblingPageSize = 5;

            var current = await GetMessageAsync(messageId);
            if (current == null) return null;

            var conversation = await GetConversationAsync(current.ConversationId);
            var chain = new List<Message> { current };
            var cursor = current;
            var guard = 0;
            while (cursor.ReplyTo != null && guard++ < 64)
            {
                var parent = await GetMessageAsync(cursor.ReplyTo.Value);
                if (parent == null) break;
                chain.Add(parent);
                cursor = parent;
            }

            chain.Reverse(); // root → … → target
            var root = chain[0];
            var parentId = current.ReplyTo;
            var ancestors = chain.Take(chain.Count - 1).Select(m => m.Id).ToList();

            var path = new List<MessagePathNode>();
            foreach (var node in chain)
            {
                var nodeParent = node.ReplyTo;
                var pageSize = nodeParent == null ? rootPageSize : siblingPageSize;
                var pageIndex = await _conversationsDataProvider.GetMessagePageIndexAsync(
                    current.ConversationId, nodeParent, node.Id, pageSize);
                path.Add(new MessagePathNode
                {
                    MessageId = node.Id,
                    ParentId = nodeParent,
                    PageIndex = pageIndex,
                });
            }

            return new MessageLocation
            {
                MessageId = current.Id,
                ConversationId = current.ConversationId,
                EventId = conversation?.EventId,
                RootId = root.Id,
                ParentId = parentId,
                Path = path,
                AncestorIds = ancestors,
                RootPageIndex = path[0].PageIndex,
                SiblingPageIndex = parentId == null ? 0 : path[^1].PageIndex,
            };
        }

        public async Task UpdateMessageAsync(MessageRequest message)
        {
            var mappedRequest = _mapper.Map<MessageDto>(message);
            await _conversationsDataProvider.UpdateMessageAsync(mappedRequest);
        }

        public async Task<List<Guid>> GetConversationAuthorAccountIdsAsync(Guid conversationId)
        {
            return await _conversationsDataProvider.GetConversationAuthorAccountIdsAsync(conversationId);
        }

        public async Task<MessageVoteResult> SetMessageVoteAsync(Guid messageId, Guid accountId, MessageVoteValue value)
        {
            var dbValue = _mapper.Map<DbDataProvider.Models.Enums.MessageVoteValue>(value);
            var stats = await _conversationsDataProvider.SetMessageVoteAsync(messageId, accountId, dbValue);
            return MapVoteResult(stats);
        }

        public async Task<MessageVoteResult> RemoveMessageVoteAsync(Guid messageId, Guid accountId)
        {
            var stats = await _conversationsDataProvider.RemoveMessageVoteAsync(messageId, accountId);
            return MapVoteResult(stats);
        }

        public async Task ApplyMessageVoteStatsAsync(IEnumerable<Message> messages, Guid? currentAccountId)
        {
            var list = messages?.ToList();
            if (list == null || list.Count == 0)
                return;

            var stats = await _conversationsDataProvider.GetMessageVoteStatsAsync(
                list.Select(m => m.Id).ToList(),
                currentAccountId);

            foreach (var message in list)
            {
                if (!stats.TryGetValue(message.Id, out var item))
                    continue;

                message.LikesCount = item.LikesCount;
                message.DislikesCount = item.DislikesCount;
                message.CurrentUserVote = item.CurrentUserVote == null
                    ? null
                    : _mapper.Map<MessageVoteValue>(item.CurrentUserVote.Value);
            }
        }

        private MessageVoteResult MapVoteResult(MessageVoteStatsDto stats)
        {
            return new MessageVoteResult
            {
                MessageId = stats.MessageId,
                LikesCount = stats.LikesCount,
                DislikesCount = stats.DislikesCount,
                CurrentUserVote = stats.CurrentUserVote == null
                    ? null
                    : _mapper.Map<MessageVoteValue>(stats.CurrentUserVote.Value)
            };
        }
    }
}
