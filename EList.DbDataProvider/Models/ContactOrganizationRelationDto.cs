using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("contact_organization_rls")]
    public class ContactOrganizationRelationDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        [Column("contact_data_id")]
        public Guid ContactId { get; set; }


        [Association(ThisKey = nameof(OrganizationId), OtherKey = nameof(OrganizationDto.Id))]
        public OrganizationDto Organization { get; set; }

        [Association(ThisKey = nameof(ContactId), OtherKey = nameof(ContactDataDto.Id))]
        public ContactDataDto ContactData { get; set; }
    }
}
