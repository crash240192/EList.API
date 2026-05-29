using EList.Models.Enums;
using EList.Models.EventsRating;

namespace EList.Repositories.Interfaces
{
    public interface IEventsRatingRepository
    {
        Task<Guid> CreateEventRatingAsync(EventsRatingItem request); //создание нового голоса
        Task DeleteEventRatingAsync(Guid id); //удаление данных голосования
        Task UpdateEventRatingAsync(Guid id, int value, string comment); //обновление оценки
        Task<EventRating> GetEventRatingAcync(Guid eventId, EventRatingType eventRatingType, int? pageIndex, int? pageSize); //получение оценки определенного ивента
        Task<EventsRatingItem?> GetRatingItemAsync(Guid itemId);
    }
}
