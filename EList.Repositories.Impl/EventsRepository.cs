using AutoMapper;
using EList.Common.Models;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.Models.Events;
using EList.Models.Events.EventMetadata;
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
                EndTime = request.EndTime,
                CoverImageId = request.CoverImageId
            };
            var result = await _eventsDataProvider.CreateEventAsync(mappedRequest);
            return result;
        }

        public async Task<Event> GetEventAsync(Guid id)
        {
            var item = await _eventsDataProvider.GetEventAsync(id);

            var result = _mapper.Map<Event>(item);
            result.Types = item?.Types?.Select(i => _mapper.Map<EventType>(i.Type))?.ToList();
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
                CoverImageId = request.CoverImageId,
                Id = id
            };
            await _eventsDataProvider.UpdateEventAsync(mappedRequest);
        }

        public async Task SetEventCoverImageAsync(Guid id, Guid? imageId)
        {            
            await _eventsDataProvider.SetEventCoverImageAsync(id, imageId);
        }

        public async Task<PagedList<Event>> SearchEventsAsync(EventsSearchRequest request, Guid? curAccountId)
        {
            var mappedRequest = _mapper.Map<DbDataProvider.Models.SearchRequests.EventsSearchRequest>(request);
            var items = await _eventsDataProvider.SearchEventsAsync(mappedRequest, curAccountId);

            var resultList = items.Items?.Select(i =>
            {
                var item = _mapper.Map<Event>(i);
                item.Types = i.Types.Select(i => i.Type).Select(i => _mapper.Map<EventType>(i)).ToList();
                return item;
            }).ToList();

            return new PagedList<Event>(items.TotalCount, resultList, request.PageIndex, request.PageSize);
        }

        public async Task CancelEventAsync(Guid eventId)
        {
            await _eventsDataProvider.CancelEventAsync(eventId);
        }
    }
}
