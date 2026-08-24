using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("organization_legal")]
    public class OrganizationLegalDto
    {
        [Column("organization_id"), PrimaryKey]
        public Guid OrganizationId { get; set; }

        [Column("legal_form", DataType = DataType.Enum)]
        public OrganizationLegalForm LegalForm { get; set; }

        [Column("inn")]
        public string? Inn { get; set; }

        [Column("inn_hash")]
        public string? InnHash { get; set; }

        [Column("ogrn")]
        public string? Ogrn { get; set; }

        [Column("kpp")]
        public string? Kpp { get; set; }

        [Column("legal_address")]
        public string? LegalAddress { get; set; }

        [Column("head_name")]
        public string? HeadName { get; set; }

        [Column("head_basis")]
        public string? HeadBasis { get; set; }

        [Column("verified_at")]
        public DateTimeOffset? VerifiedAt { get; set; }


        [Association(ThisKey = nameof(OrganizationId), OtherKey = nameof(OrganizationDto.Id))]
        public OrganizationDto Organization { get; set; }
    }
}
