using AutoMapper;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.Enums;
using EList.Models.EventsRating;
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
            var resultList = _mapper.Map<List<EventsRatingItem>>(items.Item3);
            return new EventRating(items.Item1, items.Item2, resultList, pageIndex ?? 1, pageSize ?? items.Item1);
        }
    }
}