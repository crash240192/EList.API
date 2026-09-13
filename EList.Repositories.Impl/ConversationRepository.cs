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
