using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("public.contact_account_rls")]
    public class ContactAccountRelationDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("account_id")]
        public Guid AccountId { get; set; }

        [Column("contact_data_id")]
        public Guid ContactId { get; set; }


        [Association(ThisKey = nameof(AccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto Account { get; set; }

        [Association(ThisKey = nameof(ContactId), OtherKey = nameof(ContactDataDto.Id))]
        public ContactDataDto ContactData { get; set; }
    }
}
