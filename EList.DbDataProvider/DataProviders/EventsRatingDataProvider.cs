using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;
using EList.Models.EventsRating;
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
            var existingRating = await _connection.EventsRating.FirstOrDefaultAsync(i => i.EventId == request.EventId && i.AccountId == request.AccountId && i.RatingType == request.RatingType);
            if (existingRating != null)
            {
                var newRating = await _connection.EventsRating
                .Where(i => i.Id == existingRating.Id)
                .Set(i => i.Value, request.Value)
                .Set(i => i.Comment, request.Comment)
                .UpdateAsync();
                return existingRating.Id;
            }
            else
            {
                var result = (Guid)await _connection.InsertWithIdentityAsync(request);
                return result;
            }
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
                .LoadWith(i => i.Account)
                .ThenLoad(i => i.Avatars)
                .Where(i => i.EventId == eventId && i.RatingType == eventRatingType);
            var total = await request.CountAsync();
            var average = await request.AverageAsync(i => (double?)i.Value);

            List<EventsRatingDto> result;
            if (pageIndex != null && pageSize != null)
                result = await request.Skip(pageSize.Value * pageIndex.Value).Take(pageSize.Value).ToListAsync();
            else
                result = await request.ToListAsync();

            return new ValuedListResponse<EventsRatingDto>(total, average, result);
        }

        public async Task<EventsRatingDto?> GetRatingItemAsync(Guid itemId)
        {
            var item = await _connection.EventsRating.FirstOrDefaultAsync(i => i.Id == itemId);
            return item;
        }

        public async Task<EventsRatingDto?> GetAccountEventRatingAsync(Guid eventId, Guid accountId, EventRatingType ratingType)
        {
            return await _connection.EventsRating.FirstOrDefaultAsync(i =>
                i.EventId == eventId
                && i.AccountId == accountId
                && i.RatingType == ratingType);
        }

        public async Task<int> CountRatingsForEventAsync(Guid eventId)
        {
            return await _connection.EventsRating.CountAsync(i => i.EventId == eventId);
        }

        public async Task UpdateEventRatingAsync(Guid id, int value, string comment)
        {
            var localvalue = await _connection.EventsRating
                .Where(i => i.Id == id)
                .Set(i => i.Value, value)
                .Set(i => i.Comment, comment)
                .UpdateAsync();
        }

        public async Task<double?> GetOrganizatorRatingAsync(Guid accountId)
        {
            // Average event ratings for events where this account is an organizer.
            // Join EventsRating explicitly: Event.Rating is a 1:N association and
            // Average over the singular nav property returns NULL when multiple rows exist.
            var result = await _connection.EventsRating
                .Where(r => _connection.Organizators.Any(o =>
                    o.AccountId == accountId && o.EventId == r.EventId))
                .AverageAsync(r => (double?)r.Value);
            return result;
        }

        public async Task<double?> GetOrganizationRatingAsync(Guid organizationId)
        {
            // Same aggregate as account organizer rating, filtered by organization host.
            var result = await _connection.EventsRating
                .Where(r => _connection.Organizators.Any(o =>
                    o.OrganizationId == organizationId && o.EventId == r.EventId))
                .AverageAsync(r => (double?)r.Value);
            return result;
        }
    }
}
