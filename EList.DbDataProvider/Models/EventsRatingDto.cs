using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("events_rating")]
    public class EventsRatingDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("voter_id")]
        public Guid AccountId { get; set; }

        [Column("event_id")]
        public Guid EventId { get; set; }

        [Column("comment")]
        public string Comment { get; set; }

        [Column("value")]
        public int Value { get; set; }

        [Column("rating_type", DataType = DataType.Enum)]
        public EventRatingType RatingType { get; set; }

        [Association(ThisKey = nameof(AccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto Account { get; set; }

        [Association(ThisKey = nameof(EventId), OtherKey = nameof(EventDto.Id))]
        public EventDto Event { get; set; }
    }
}