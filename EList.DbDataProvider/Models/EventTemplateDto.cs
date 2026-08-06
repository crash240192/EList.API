using LinqToDB;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("event_templates")]
    public class EventTemplateDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("owner_account_id")]
        public Guid? OwnerAccountId { get; set; }

        [Column("owner_organization_id")]
        public Guid? OwnerOrganizationId { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("template_body"), DataType("jsonb")]
        public string TemplateBody { get; set; }

        [Column("create_date")]
        public DateTimeOffset CreateDate { get; set; }

        [Column("update_date")]
        public DateTimeOffset UpdateDate { get; set; }


        [Association(ThisKey = nameof(OwnerAccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto? OwnerAccount { get; set; }

        [Association(ThisKey = nameof(OwnerOrganizationId), OtherKey = nameof(OrganizationDto.Id))]
        public OrganizationDto? OwnerOrganization { get; set; }
    }
}
