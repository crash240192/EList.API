using EList.DbDataProvider.Models;

namespace EList.DbDataProvider.Interfaces
{
    public interface IEventsMetadataDataProvider
    {
        Task<Guid> CreateEventCategoryAsync(EventCategoryDto request);
        Task UpdateEventCategoryAsync(EventCategoryDto request);
        Task DeleteEventCategoryAsync(Guid id);
        Task<EventCategoryDto?> GetEventCategoryAsync(Guid id);
        Task<List<EventCategoryDto>> GetAllEventCategoriesAsync();

        Task<Guid> CreateEventTypeAsync(EventTypeDto request);
        Task UpdateEventTypeAsync(EventTypeDto request);
        Task DeleteEventTypeAsync(Guid id);
        Task<EventTypeDto?> GetEventTypeAsync(Guid id);
        Task<List<EventTypeDto>?> GetAllEventTypesAsync();
        Task<List<EventTypeDto>?> GetEventTypesByEventIdAsync(Guid eventId);
        Task<List<EventTypeDto>?> GetEventTypesByCategoryIdAsync(Guid categoryId);
        Task BindEventTypesAsync(Guid eventId, List<Guid> eventTypeIds);

        Task<Guid> CreateEventParametersAsync(EventParametersDto request);
        Task DeleteEventParametersAsync(Guid id);
        Task UpdateEventParametersAsync(EventParametersDto request);
        Task<EventParametersDto?> GetEventParametersByEventIdAsync(Guid eventId);
        Task<EventParametersDto?> GetEventParametersAsync(Guid id);
        Task BindEventParametersAsync(Guid eventId, Guid eventParametersId);
    }
}
