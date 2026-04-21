using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
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

        public async Task<List<AccountDto>> GetEventParticipantsAsync(Guid eventId)
        {
            var accounts = await _connection.Participations
                .LoadWith(i => i.Account)
                .ThenLoad(i => i.PersonInfo)
                .Where(i => eventId == i.EventId)
                .Select(i => i.Account)
                .ToListAsync();
            return accounts;
        }
    }
}
