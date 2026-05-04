using EList.Models.Accounts;
using EList.Models.Enums;
using EList.Models.Person;

namespace EList.Models.EventsRating
{
    public class EventsRatingItem
    {
        /// <summary>
        /// Идентификатор записи
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Идентификатор пользователя
        /// </summary>
        public Guid AccountId { get; set; }

        /// <summary>
        /// Идентификатор мероприятия
        /// </summary>
        public Guid EventId { get; set; }

        /// <summary>
        /// Комментарий
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Значение
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// Тип голоса
        /// </summary>
        public EventRatingType RatingType { get; set; }

        public AccountPublicData Account { get; set; }
        public PersonInfo PersonInfo { get; set; }
    }
}
