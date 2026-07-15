using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("anonymous_age_agreements")]
    public class AnonymousAgeAgreementDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("jwt")]
        public string Jwt { get; set; }

        [Column("agreement_date")]
        public DateTimeOffset AgreementDate{ get; set; }

        [Column("client_info")]
        public string ClientInfo { get; set; }
    }
}
