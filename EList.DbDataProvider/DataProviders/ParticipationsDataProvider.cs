using EList.Common.Extensions;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.SearchRequests;
using EList.Models.Invitations;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class ParticipationsDataProvider : DataProviderBase, IParticipationsDataProvider
    {

        public ParticipationsDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }

        public async Task LeaveEventAsync(Guid accountId, Guid eventId)
        {
            var existingParticipation = await _connection.Participations.FirstOrDefaultAsync(i => i.AccountId == accountId && eventId == i.EventId);

            if (existingParticipation != null)
            {
                await _connection.DeleteAsync(existingParticipation);
            }
        }

        public async Task<Guid> ParticipateAsync(Guid accountId, Guid eventId)
        {
            var existingParticipation = await _connection.Participations.FirstOrDefaultAsync(i => i.AccountId == accountId && eventId == i.EventId);

            Guid result;
            if (existingParticipation == null)
            {
                result = (Guid)await _connection.InsertWithIdentityAsync(new ParticipationDto
                {
                    AccountId = accountId,
                    EventId = eventId
                });
            }
            else
            {
                result = existingParticipation.Id;
            }

            return result;
        }

        public async Task<ListResponse<AccountDto>> GetEventParticipantsAsync(EventParticipantsSearchRequest request)
        {
            var accountsRequest = _connection.Participations
                .LoadWith(i => i.Account)
                .ThenLoad(i => i.PersonInfo)
                .Where(i => request.EventId == i.EventId)                
                .OrderBy(i => i.Account.Login)
                .Select(i => i.Account);

            #region name
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var splitNameSubscrings = request.Name.ToLower().Split(' ');
                accountsRequest = accountsRequest.Where(i => splitNameSubscrings.All(nameItem => 
                i.Login.ToLower().Contains(nameItem)
                || (!string.IsNullOrWhiteSpace(i.PersonInfo.FirstName)
                    ? i.PersonInfo.FirstName.ToLower().Contains(nameItem)
                    : false)
                || (!string.IsNullOrWhiteSpace(i.PersonInfo.LastName)
                    ? i.PersonInfo.LastName.ToLower().Contains(nameItem)
                    : false)
                || (!string.IsNullOrWhiteSpace(i.PersonInfo.Patronymic)
                    ? i.PersonInfo.Patronymic.ToLower().Contains(nameItem)
                    : false)
                ));
            }
            #endregion
            if (request.Gender != null)
                accountsRequest = accountsRequest.Where(i => i.PersonInfo.Gender == request.Gender);

            if (request.Age != null)
                accountsRequest = accountsRequest.Where(i => i.PersonInfo.Birthdate >= DateTime.Now.AddYears(-request.Age.Value));

            #region subscriptions
            if (request.SubscribedToId != null) // Список участников, подписанных на этого пользователя
            {
                accountsRequest = accountsRequest.LoadWith(i => i.Subscriptions)
                    .Where(i => i.Subscriptions.Any(s => s.SubscribedToId == request.SubscribedToId));
            }

            if (request.SubscriberId != null) // Список участников на которых подписан этот пользователь
            {
                accountsRequest = accountsRequest.LoadWith(i => i.Subscribers)
                    .Where(i => i.Subscribers.Any(s => s.SubscriberId == request.SubscriberId));
            }
            #endregion

            var count = await accountsRequest.CountAsync();

            List<AccountDto> resultList;
            if (request.PageSize != null && request.PageIndex != null)
                resultList = await accountsRequest.Skip(request.PageSize.Value * request.PageIndex.Value).Take(request.PageSize.Value).ToListAsync();
            else
                resultList = await accountsRequest.ToListAsync();

            return new ListResponse<AccountDto>(count, resultList);
        }

        public async Task<int> GetParticipantsCountAsync(Guid eventId)
        {
            var count = await _connection.Participations.Where(i => i.EventId == eventId)
                .CountAsync();
            return count;
        }
    }
}
