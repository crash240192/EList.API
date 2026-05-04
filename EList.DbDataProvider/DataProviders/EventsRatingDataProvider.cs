using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Async;

namespace EList.DbDataProvider.DataProviders
{
    public class EventsRatingDataProvider : DataProviderBase, IEventsRatingDataProvider
    {
        public EventsRatingDataProvider(IDataConnectionProvider dataConnectionProvider) : base(dataConnectionProvider)
        {
        }

        public async Task<Guid> CreateEventRatingAsync(EventsRatingDto request)
        {
            var result = (Guid)await _connection.InsertWithIdentityAsync(request);
            return result;
        }

        public async Task DeleteEventRatingAsync(Guid id)
        {
            await _connection.EventsRating.DeleteAsync(i => i.Id == id);
        }

        public async Task<ValuedListResponse<EventsRatingDto>> GetEventRatingAsync(Guid eventId, EventRatingType eventRatingType, int? pageIndex, int? pageSize)
        {
            var request = _connection.EventsRating
                .LoadWith(i => i.Account)
                .ThenLoad(i => i.PersonInfo)
                .Where(i => i.EventId == eventId && i.RatingType == eventRatingType);
            var total = await request.CountAsync();
            var average = await request.AverageAsync(i => (double?)i.Value);

            List<EventsRatingDto> result;
            if (pageIndex != null && pageSize != null)
                result = await request.Skip(pageSize.Value * pageIndex.Value).Take(pageSize.Value).ToListAsync();
            else
                result = await request.ToListAsync();

            return new ValuedListResponse<EventsRatingDto> (total, average, result);
        }

        public async Task UpdateEventRatingAsync(Guid id, int value, string comment)
        {
            var localvalue = await _connection.EventsRating
                .Where(i => i.Id == id)
                .Set(i => i.Value, value)
                .Set(i => i.Comment, comment)
                .UpdateAsync();
        }
    }
}
