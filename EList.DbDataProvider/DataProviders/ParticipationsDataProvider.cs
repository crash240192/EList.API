using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using LinqToDB;

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

        public async Task<List<PersonInfoDto>> GetEventParticipantsAsync(Guid eventId)
        {
            var accountIds = await _connection.Participations.Where(i => eventId == i.EventId).Select(i => i.AccountId).ToListAsync();
            var persons = await _connection.Persons.Where(i => accountIds.Contains(i.AccountId)).ToListAsync();
            return persons;
        }
    }
}
