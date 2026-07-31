using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("organization_agreement_rls")]
    public class OrganizationAgreementDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        [Column("document_id")]
        public Guid DocumentId { get; set; }

        [Column("agreement_date")]
        public DateTimeOffset AgreementDate { get; set; }


        [Association(ThisKey = nameof(DocumentId), OtherKey = nameof(DocumentDto.Id))]
        public DocumentDto Document { get; set; }

        [Association(ThisKey = nameof(OrganizationId), OtherKey = nameof(OrganizationDto.Id))]
        public OrganizationDto Organization { get; set; }
    }
}
