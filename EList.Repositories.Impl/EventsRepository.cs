using AutoMapper;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.Events;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class EventsRepository : IEventsRepository
    {
        private readonly IEventsDataProvider _eventsDataProvider;
        private readonly IMapper _mapper;

        public EventsRepository(IEventsDataProvider eventsDataProvider,
            IMapper mapper)
        {
            _eventsDataProvider = eventsDataProvider;
            _mapper = mapper;
        }

        public async Task<Guid> CreateEventAsync(EventRequest request)
        {
            var mappedRequest = new EventDto
            {
                Active = request.Active,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Name = request.Name,
                StartTime = request.StartTime,
                UpdateDate = DateTimeOffset.Now.ToUniversalTime(),
                CreateDate = DateTimeOffset.Now.ToUniversalTime(),
                Address = request.Address,
                Description = request.Description,
                EndTime = request.EndTime
            };
            var result = await _eventsDataProvider.CreateEventAsync(mappedRequest);
            return result;
        }

        public async Task<Event> GetEventAsync(Guid id)
        {
            var item = await _eventsDataProvider.GetEventAsync(id);

            var result = _mapper.Map<Event>(item);
            return result;
        }

        public async Task UpdateEventAsync(Guid id, EventRequest request)
        {
            var mappedRequest = new EventDto
            {
                Active = request.Active,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Name = request.Name,
                StartTime = request.StartTime,
                UpdateDate = DateTimeOffset.Now.ToUniversalTime(),
                CreateDate = DateTimeOffset.Now.ToUniversalTime(),
                Address = request.Address,
                Description = request.Description,
                EndTime = request.EndTime,
                Id = id
            };
            await _eventsDataProvider.UpdateEventAsync(mappedRequest);
        }

        public async Task<PagedList<Event>> SearchEventsAsync(EventsSearchRequest request)
        {
            var mappedRequest = _mapper.Map<DbDataProvider.Models.SearchRequests.EventsSearchRequest>(request);
            var items = await _eventsDataProvider.SearchEventsAsync(mappedRequest);
            var resultList = _mapper.Map<List<Event>>(items.Item2);
            return new PagedList<Event>(items.Item1, resultList, request.PageIndex, request.PageSize);
        }
    }
}
