using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("tariff_validators")]
    public class TariffValidatorDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("cost_limit")]
        public double? CostLimit { get; set; }

        [Column("persons_limit")]
        public int? PersonsLimit { get; set; }

        [Column("allow_private")]
        public bool AllowPrivate { get; set; }

        [Column("age_limit")]
        public int? AgeLimit { get; set; }

        [Column("max_events_count")]
        public int? MaxEventsCount { get; set; }

        [Column("max_period")]
        public int? CreateDateMaxPeriod { get; set; }

        [Column("allow_multidays_events")]
        public bool AllowMultidaysEvent {  get; set; }

        [Column("allow_gender_segregation")]
        public bool? AllowGenderSegregation { get; set; }
    }
}
