using EList.Models.Events.EventMetadata;

namespace EList.Repositories.Interfaces
{
    public interface IEventsMetadataRepository
    {
        Task<Guid> CreateEventCategoryAsync(EventCategoryRequest request);
        Task UpdateEventCategoryAsync(Guid id, EventCategoryRequest request);
        Task DeleteEventCategoryAsync(Guid id);
        Task<EventCategory?> GetEventCategoryAsync(Guid id);
        Task<List<EventCategory>?> GetAllEventCategoriesAsync();

        Task<Guid> CreateEventTypeAsync(EventTypeRequest request);
        Task UpdateEventTypeAsync(Guid id, EventTypeRequest request);
        Task DeleteEventTypeAsync(Guid id);
        Task<EventType?> GetEventTypeAsync(Guid id);
        Task<List<EventType>?> GetAllEventTypesAsync();
        Task<List<EventType>?> GetEventTypesByEventIdAsync(Guid eventId);
        Task<List<EventType>?> GetEventTypesByCategoryIdAsync(Guid categoryId);
        Task BindEventTypesAsync(Guid eventId, List<Guid> eventTypeIds);

        Task<Guid> CreateEventParametersAsync(EventParametersRequest request);
        Task DeleteEventParametersAsync(Guid id);
        Task UpdateEventParametersAsync(Guid id, EventParametersRequest request);
        Task<EventParameters?> GetEventParametersByEventIdAsync(Guid eventId);
        Task<EventParameters?> GetEventParametersAsync(Guid id);
        Task BindEventParametersAsync(Guid eventId, Guid eventParametersId);
    }
}
