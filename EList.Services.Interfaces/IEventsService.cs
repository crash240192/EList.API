using EList.Common.Models;
using EList.Models.Events;
using EList.Models.Events.EventMetadata;

namespace EList.Services.Interfaces
{
    public interface IEventsService
    {
        Task<CommandResult<Guid?>> CreateEventCategoryAsync(EventCategoryRequest request);
        Task<CommandResult> UpdateEventCategoryAsync(Guid id, EventCategoryRequest request);
        Task<CommandResult> DeleteEventCategoryAsync(Guid id);
        Task<CommandResult<EventCategory?>> GetEventCategoryAsync(Guid id);
        Task<CommandResult<List<EventCategory>?>> GetAllEventCategoriesAsync();

        Task<CommandResult<Guid?>> CreateEventTypeAsync(EventTypeRequest request);
        Task<CommandResult> UpdateEventTypeAsync(Guid id, EventTypeRequest request);
        Task<CommandResult> DeleteEventTypeAsync(Guid id);
        Task<CommandResult<EventType?>> GetEventTypeAsync(Guid id);
        Task<CommandResult<List<EventType>?>> GetAllEventTypesAsync();
        Task<CommandResult<List<EventType>?>> GetEventTypesByEventIdAsync(Guid eventId);
        Task<CommandResult<List<EventType>?>> GetEventTypesByCategoryIdAsync(Guid categoryId);
        Task<CommandResult> SetEventTypesAsync(Guid eventId, List<Guid> typeIds);

        //Task<CommandResult<Guid?>> CreateEventParametersAsync(EventParametersRequest request);
        //Task<CommandResult> DeleteEventParametersAsync(Guid id);
        //Task<CommandResult> UpdateEventParametersAsync(Guid id, EventParametersRequest request);
        //Task<CommandResult<EventParameters?>> GetEventParametersAsync(Guid id);

        Task<CommandResult<EventParameters?>> GetEventParametersByEventIdAsync(Guid eventId);
        Task<CommandResult> SetEventParametersAsync(Guid eventId, EventParametersRequest parameters);
        

        Task<CommandResult<Guid?>> CreateEventAsync(CreateEventRequest request);
        Task<CommandResult> UpdateEventAsync(Guid eventId, EventRequest request);
        Task<CommandResult> SetEventCoverImageAsync(Guid eventId, Guid? imageId);
        Task<CommandResult<Event>> GetEventAsync(Guid id);
        Task<CommandResult<PagedList<Event>?>> SearchEventsAsync(EventsSearchRequest request);
        Task<CommandResult> CancelEventAsync(Guid eventId);
    }
}
