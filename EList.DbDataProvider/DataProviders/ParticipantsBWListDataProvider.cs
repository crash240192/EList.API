using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class ParticipantsBWListDataProvider : DataProviderBase, IParticipantsBWListDataProvider
    {

        public ParticipantsBWListDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        { }


        public async Task<Guid> AddToBlackListAsync(Guid eventId, Guid accountId)
        {
            var existingItem = await _connection.BlackList.FirstOrDefaultAsync(i =>  i.EventId == eventId && i.AccountId == accountId);
            if (existingItem != null)
                return existingItem.Id;
            
            var result = (Guid) await _connection.InsertWithIdentityAsync(new ParticipantsBlackListItemDto
            {
                EventId = eventId,
                AccountId = accountId
            });

            return result;
        }

        public async Task<Guid> AddToWhiteListAsync(Guid eventId, Guid accountId)
        {
            var existingItem = await _connection.WhiteList.FirstOrDefaultAsync(i => i.EventId == eventId && i.AccountId == accountId);
            if (existingItem != null)
                return existingItem.Id;

            var result = (Guid)await _connection.InsertWithIdentityAsync(new ParticipantsWhiteListItemDto
            {
                EventId = eventId,
                AccountId = accountId
            });

            return result;
        }


        public async Task<bool> IsUserInBlackListAsync(Guid eventId, Guid accountId)
        {
            var result = await _connection.BlackList.AnyAsync(i => i.EventId == eventId &&i.AccountId == accountId);
            return result;
        }

        public async Task<bool> IsUserInWhiteListAsync(Guid eventId, Guid accountId)
        {
            var result = await _connection.WhiteList.AnyAsync(i => i.EventId == eventId && i.AccountId == accountId);
            return result;
        }


        public async Task DeleteFromBlackListAsync(Guid eventId, Guid accountId)
        {
            await _connection.BlackList.DeleteAsync(i => i.EventId == eventId && i.AccountId == accountId);
        }

        public async Task DeleteFromWhiteListAsync(Guid eventId, Guid accountId)
        {
            await _connection.WhiteList.DeleteAsync(i => i.EventId == eventId && i.AccountId == accountId);
        }


        public async Task<ListResponse<ParticipantsBlackListItemDto>> GetEventBlackListAsync(Guid eventId, int? pageIndex, int? pageSize)
        {
            var request = _connection.BlackList
                .LoadWith(i => i.Account)
                .ThenLoad(i => i.PersonInfo)
                .Where(i => i.EventId == eventId);

            var count = await request.CountAsync();

            var resultList = new List<ParticipantsBlackListItemDto>();

            if (pageSize != null && pageIndex != null)
                resultList = await request.Skip(pageSize.Value * pageIndex.Value).Take(pageSize.Value).ToListAsync();
            else
                resultList = await request.ToListAsync();

            return new ListResponse<ParticipantsBlackListItemDto>(count, resultList);
        }

        public async Task<ListResponse<ParticipantsWhiteListItemDto>> GetEventWhiteListAsync(Guid eventId, int? pageIndex, int? pageSize)
        {
            var request = _connection.WhiteList
                .LoadWith(i => i.Account)
                .ThenLoad(i => i.PersonInfo)
                .Where(i => i.EventId == eventId);

            var count = await request.CountAsync();

            var resultList = new List<ParticipantsWhiteListItemDto>();

            if (pageSize != null && pageIndex != null)
                resultList = await request.Skip(pageSize.Value * pageIndex.Value).Take(pageSize.Value).ToListAsync();
            else
                resultList = await request.ToListAsync();

            return new ListResponse<ParticipantsWhiteListItemDto>(count, resultList);
        }
    }
}
