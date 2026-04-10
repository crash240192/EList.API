using AutoMapper;
using EList.DbDataProvider.DataProviders;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.Events;
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

        public async Task<Guid> CreateEventRatingAsync(EventsRating request)
        {
            var mappedRequest = new EventsRatingDto
            {
                Id = request.Id,
                AccountId = request.AccountId,
                EventId = request.EventId,
                Comment = request.Comment,
                Value = request.Value,
                RatingType = (DbDataProvider.Models.Enums.EventRatingType)request.RatingType
                
            };
            var result = await _eventsRatingDataProvider.CreateEventRatingAsync(mappedRequest);
            return result;
        }

        public async Task DeleteEventRatingAsync(Guid id)
        {
            await _eventsRatingDataProvider.DeleteEventRatingAsync(id);
            
        }

        public async Task UpdateEventRatingAsync(Guid id, int value)
        {
            var mappedRequest = new EventsRatingDto
            {
                Value = value

            };
             await _eventsRatingDataProvider.UpdateEventRatingAsync(id, mappedRequest);
        }

        public async Task UpdateEventRatingAsync(Guid id, string comment)
        {
            var mappedRequest = new EventsRatingDto
            {
                Comment = comment

            };
            await _eventsRatingDataProvider.UpdateEventRatingAsync(id, mappedRequest);
        }

        public async Task<List<EventsRating>> GetEventRatingAcync(Guid eventID)
        {
            var items = await _eventsRatingDataProvider.GetEventRatingAsync(eventID);
            var result = _mapper.Map<List<EventsRating>>(items);
            return result;
        }
    }
}