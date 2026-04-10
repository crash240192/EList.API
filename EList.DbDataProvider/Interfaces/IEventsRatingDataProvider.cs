using EList.DbDataProvider.Models;
using EList.DbDataProvider.Models.Enums;

namespace EList.DbDataProvider.Interfaces
{
    public interface IEventsRatingDataProvider
    {
        Task<Guid> CreateEventRatingAsync(EventsRatingDto request); //создание нового голоса
        Task DeleteEventRatingAsync(Guid id); //удаление данных голосования
        Task UpdateEventRatingAsync(Guid id, int value); //обновление оценки
        Task UpdateEventRatingAsync(Guid id, string comment); //обновление комментария 
        Task<List<EventsRatingDto>> GetEventRatingAsync(Guid eventID); //получение оценки определенного ивента
        Task UpdateEventRatingAsync(Guid id, EventsRatingDto mappedRequest);

    }
}
