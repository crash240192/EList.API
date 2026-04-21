using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("tariffs")]
    public class TariffDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("cost")]
        public double Cost { get; set; }

        [Column("period")]
        public TimeSpan Period { get; set; }

        [Column("validator_id")]
        public Guid ValidatorId { get; set; }

        [Association(ThisKey = nameof(ValidatorId), OtherKey = nameof(TariffValidatorDto.Id))]
        public TariffValidatorDto TariffValidator { get; set; }
    }
}
