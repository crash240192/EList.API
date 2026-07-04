using AutoMapper;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.Enums;
using EList.Models.EventsRating;
using EList.Models.Person;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class EventsRatingRepository : IEventsRatingRepository
    {
        private readonly IEventsRatingDataProvider _eventsRatingDataProvider;
        private readonly IMapper _mapper;

        public EventsRatingRepository(IEventsRatingDataProvider eventsRatingDataProvider,
            IMapper mapper)
        {
            _eventsRatingDataProvider = eventsRatingDataProvider;
            _mapper = mapper;
        }

        public async Task<Guid> CreateEventRatingAsync(EventsRatingItem request)
        {
            var mappedRequest = _mapper.Map<EventsRatingDto>(request);

            var result = await _eventsRatingDataProvider.CreateEventRatingAsync(mappedRequest);
            return result;
        }

        public async Task DeleteEventRatingAsync(Guid id)
        {
            await _eventsRatingDataProvider.DeleteEventRatingAsync(id);
        }

        public async Task UpdateEventRatingAsync(Guid id, int value, string comment)
        {
            await _eventsRatingDataProvider.UpdateEventRatingAsync(id, value, comment);
        }

        public async Task<EventRating> GetEventRatingAcync(Guid eventId, EventRatingType eventRatingType, int? pageIndex, int? pageSize)
        {
            var items = await _eventsRatingDataProvider.GetEventRatingAsync(eventId, _mapper.Map<DbDataProvider.Models.Enums.EventRatingType>(eventRatingType), pageIndex, pageSize);
            var resultList = items.Items?.Select(i =>
            {
                var result = _mapper.Map<EventsRatingItem>(i);
                result.PersonInfo = _mapper.Map<PersonInfo>(i.Account.PersonInfo);
                return result;
            })?.ToList();
            return new EventRating(items.TotalCount, items.Value, resultList, pageIndex ?? 1, pageSize ?? items.TotalCount);
        }

        public async Task<EventsRatingItem?> GetRatingItemAsync(Guid itemId)
        {
            var item = await _eventsRatingDataProvider.GetRatingItemAsync(itemId);
            var mappedResult = _mapper.Map<EventsRatingItem?>(item);
            return mappedResult;
        }

        public async Task<double?> GetOrganizatorRatingAsync(Guid accountId)
        {
            var result = await _eventsRatingDataProvider.GetOrganizatorRatingAsync(accountId);
            return result;
        }
    }
}