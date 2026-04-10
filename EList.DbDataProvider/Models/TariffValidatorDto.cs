using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EList.DbDataProvider.Models
{
    [Table("public.tariff_validators")]
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

        [Column("allow_gender_segregation")]
        public bool? AllowGenderSegregation { get; set; }
    }
}
