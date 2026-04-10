using EList.Models.EventsRating;

namespace EList.Repositories.Interfaces
{
    public interface IEventsRatingRepository
    {
        Task<Guid> CreateEventRatingAsync(EventsRating request); //создание нового голоса
        Task DeleteEventRatingAsync(Guid id); //удаление данных голосования
        Task UpdateEventRatingAsync(Guid id, int value); //обновление оценки
        Task UpdateEventRatingAsync(Guid id, string comment); //обновление комментария 
        Task<List<EventsRating>> GetEventRatingAcync(Guid eventID); //получение оценки определенного ивента
    }
}
