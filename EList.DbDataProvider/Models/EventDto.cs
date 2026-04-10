using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("public.events")]
    public class EventDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("start_time")]
        public DateTimeOffset StartTime { get; set; }

        [Column("end_time")]
        public DateTimeOffset EndTime { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("latitude")]
        public double Latitude { get; set; }

        [Column("longitude")]
        public double Longitude { get; set; }

        [Column("address")]
        public string Address { get; set; }

        [Column("active")]
        public bool Active { get; set; }

        [Column("event_parameters_id")]
        public Guid? EventParametersId { get; set; }
        
        [Column("create_date")]
        public DateTimeOffset CreateDate { get; set; }

        [Column("update_date")]
        public DateTimeOffset UpdateDate { get; set; }


        [Association(ThisKey = nameof(EventParametersId), OtherKey = nameof(EventParametersDto.Id))]
        public EventParametersDto Parameters { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(EventsRatingDto.EventId))]
        public EventsRatingDto Rating { get; set; }
    }
}
