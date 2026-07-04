using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;

namespace EList.DbDataProvider.Interfaces
{
    public interface IEventsRatingDataProvider
    {
        Task<Guid> CreateEventRatingAsync(EventsRatingDto request); //создание нового голоса
        Task DeleteEventRatingAsync(Guid id); //удаление данных голосования
        Task UpdateEventRatingAsync(Guid id, int value, string comment); //обновление оценки
        Task<ValuedListResponse<EventsRatingDto>> GetEventRatingAsync(Guid eventId, EventRatingType eventRatingType, int? pageIndex, int? pageSize); //получение оценки определенного ивента
        Task<EventsRatingDto?> GetRatingItemAsync(Guid itemId);
        Task<double?> GetOrganizatorRatingAsync(Guid accountId);
    }
}
