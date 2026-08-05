using AutoMapper;
using EList.DbDataProvider.Interfaces;
using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;
using EList.Models.Events.EventMetadata;
using EList.Repositories.Interfaces;

namespace EList.Repositories.Impl
{
    public class EventsMetadataRepository : IEventsMetadataRepository
    {
        private readonly IEventsMetadataDataProvider _eventsMetadataDataProvider;
        private readonly IMapper _mapper;

        public EventsMetadataRepository(IEventsMetadataDataProvider eventsMetadataDataProvider,
            IMapper mapper)
        {
            _eventsMetadataDataProvider = eventsMetadataDataProvider;
            _mapper = mapper;
        }

        #region eventType
        public async Task<Guid> CreateEventTypeAsync(EventTypeRequest request)
        {
            var mappedRequest = new EventTypeDto
            {
                Description = request.Description,
                EventCategoryId = request.EventCategoryId,
                Ico = request.Ico,
                Name = request.Name,
                LocalizationPath = request.LocalizationPath
            };
            var result = await _eventsMetadataDataProvider.CreateEventTypeAsync(mappedRequest);
            return result;
        }

        public async Task DeleteEventTypeAsync(Guid id)
        {
            await _eventsMetadataDataProvider.DeleteEventTypeAsync(id);
        }

        public async Task<List<EventType>?> GetAllEventTypesAsync()
        {
            var types = await _eventsMetadataDataProvider.GetAllEventTypesAsync();
            var result = types.Select(i => _mapper.Map<EventType>(i)).ToList();
            return result;
        }

        public async Task<EventType?> GetEventTypeAsync(Guid id)
        {
            var type = await _eventsMetadataDataProvider.GetEventTypeAsync(id);
            var result = _mapper.Map<EventType>(type);
            return result;
        }

        public async Task<List<EventType>?> GetEventTypesByEventIdAsync(Guid eventId)
        {
            var types = await _eventsMetadataDataProvider.GetEventTypesByEventIdAsync(eventId);
            var result = types.Select(i => _mapper.Map<EventType>(i)).ToList();
            return result;
        }

        public async Task<List<EventType>?> GetEventTypesByCategoryIdAsync(Guid categoryId)
        {
            var types = await _eventsMetadataDataProvider.GetEventTypesByCategoryIdAsync(categoryId);
            var result = types.Select(i => _mapper.Map<EventType>(i)).ToList();
            return result;
        }

        public async Task UpdateEventTypeAsync(Guid id, EventTypeRequest request)
        {
            var mappedRequest = new EventTypeDto
            {
                Description = request.Description,
                EventCategoryId = request.EventCategoryId,
                Ico = request.Ico,
                Name = request.Name,
                LocalizationPath = request.LocalizationPath,
                Id = id
            };
            await _eventsMetadataDataProvider.UpdateEventTypeAsync(mappedRequest);
        }

        public async Task BindEventTypesAsync(Guid eventId, List<Guid> eventTypeIds)
        {
            await _eventsMetadataDataProvider.BindEventTypesAsync(eventId, eventTypeIds);
        }
        #endregion

        #region eventCategories
        public async Task<Guid> CreateEventCategoryAsync(EventCategoryRequest request)
        {
            var mappedRequest = new EventCategoryDto
            {
                Description = request.Description,
                Ico = request.Ico,
                Name = request.Name,
                LocalizationPath = request.LocalizationPath,
                Color = request.Color
            };
            var result = await _eventsMetadataDataProvider.CreateEventCategoryAsync(mappedRequest);
            return result;
        }

        public async Task DeleteEventCategoryAsync(Guid id)
        {
            await _eventsMetadataDataProvider.DeleteEventCategoryAsync(id);
        }

        public async Task<List<EventCategory>?> GetAllEventCategoriesAsync()
        {
            var categories = await _eventsMetadataDataProvider.GetAllEventCategoriesAsync();
            var result = categories.Select(i => _mapper.Map<EventCategory>(i)).ToList();
            return result;
        }

        public async Task<EventCategory?> GetEventCategoryAsync(Guid id)
        {
            var category = await _eventsMetadataDataProvider.GetEventCategoryAsync(id);
            var result = _mapper.Map<EventCategory>(category);
            return result;
        }

        public async Task UpdateEventCategoryAsync(Guid id, EventCategoryRequest request)
        {
            var mappedRequest = new EventCategoryDto
            {
                Description = request.Description,
                Ico = request.Ico,
                Id = id,
                LocalizationPath = request.LocalizationPath,
                Name = request.Name,
                Color = request.Color
            };
            await _eventsMetadataDataProvider.UpdateEventCategoryAsync(mappedRequest);
        }
        #endregion

        #region eventParameters
        public async Task<Guid> CreateEventParametersAsync(EventParametersRequest request)
        {
            var mappedRequest = new EventParametersDto
            {
                AgeLimit = request.AgeLimit,
                AllowedGender = _mapper.Map<Gender?>(request.AllowedGender),
                MaxPersonsCount = request.MaxPersonsCount,
                Private = request.Private,
                AllowUsersToInvite = request.AllowUsersToInvite,
                Cost = request.Cost,
                TicketsEnabled = request.TicketsEnabled
            };
            var result = await _eventsMetadataDataProvider.CreateEventParametersAsync(mappedRequest);
            return result;
        }

        public async Task DeleteEventParametersAsync(Guid id)
        {
            await _eventsMetadataDataProvider.DeleteEventParametersAsync(id);
        }

        public async Task UpdateEventParametersAsync(Guid id, EventParametersRequest request)
        {
            var mappedRequest = new EventParametersDto
            {
                AgeLimit = request.AgeLimit,
                AllowedGender = _mapper.Map<Gender?>(request.AllowedGender),
                MaxPersonsCount = request.MaxPersonsCount,
                Private = request.Private,
                AllowUsersToInvite = request.AllowUsersToInvite,
                Cost = request.Cost,
                TicketsEnabled = request.TicketsEnabled,
                Id = id
            };
            await _eventsMetadataDataProvider.UpdateEventParametersAsync(mappedRequest);
        }

        public async Task<EventParameters?> GetEventParametersByEventIdAsync(Guid eventId)
        {
            var parameters = await _eventsMetadataDataProvider.GetEventParametersByEventIdAsync(eventId);
            var result = _mapper.Map<EventParameters>(parameters);
            return result;
        }

        public async Task<EventParameters?> GetEventParametersAsync(Guid id)
        {
            var parameters = await _eventsMetadataDataProvider.GetEventParametersAsync(id);
            var result = _mapper.Map<EventParameters>(parameters);
            return result;
        }


        public async Task BindEventParametersAsync(Guid eventId, Guid eventParametersId)
        {
            await _eventsMetadataDataProvider.BindEventParametersAsync(eventId, eventParametersId);
        }
        #endregion
    }
}
