using EList.DbDataProvider.Models.Enums;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("event_parameters")]
    public class EventParametersDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("cost")]
        public double? Cost { get; set; }

        [Column("private")]
        public bool? Private { get; set; }

        [Column("max_persons_count")]
        public int? MaxPersonsCount { get; set; }

        [Column("age_limit")]
        public int? AgeLimit { get; set; }

        [Column("allowed_gender", DataType = LinqToDB.DataType.Enum)]
        public Gender? AllowedGender { get; set; }

        [Column("allow_users_to_invite")]
        public bool? AllowUsersToInvite { get; set; }

        [Column("tickets_enabled")]
        public bool TicketsEnabled { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(EventDto.EventParametersId))]
        public EventDto Event { get; set; }
    }
}
