using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using LinqToDB;

namespace EList.DbDataProvider.DataProviders
{
    internal class EventsRatingDataProvider : DataProviderBase, IEventsRatingDataProvider
    {
        public EventsRatingDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }

        public async Task<Guid> CreateEventRatingAsync(EventsRatingDto request)
        {
            var result = (Guid) await _connection.InsertWithIdentityAsync(request);
            return result;
        }

        public async Task DeleteEventRatingAsync(Guid id)
        {
            await _connection.EventsRating.DeleteAsync(i => i.Id == id);
        }

        public async Task<List<EventsRatingDto>> GetEventRatingAsync(Guid eventID)
        {
            var result = await _connection.EventsRating.Where(i => i.EventId == eventID).ToListAsync();
            return result;
        }

        public async Task UpdateEventRatingAsync(Guid id, int value)
        {
            //var localvalue = await _connection.EventsRating.FirstOrDefaultAsync(i => i.Id == id);
            var localvalue = await _connection.EventsRating.Where(i => i.Id == id).Set(i => i.Value, value).UpdateAsync();
        }

        public async Task UpdateEventRatingAsync(Guid id, string comment)
        {
            var localvalue = await _connection.EventsRating.Where(i => i.Id == id).Set(i => i.Comment, comment).UpdateAsync();
        }

        public Task UpdateEventRatingAsync(Guid id, EventsRatingDto mappedRequest)
        {
            throw new NotImplementedException();
        }
    }
}
