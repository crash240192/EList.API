using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("account_agreement_rls")]
    public class AccountAgreementDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("account_id")]
        public Guid AccountId { get; set; }

        [Column("document_id")]
        public Guid DocumentId { get; set; }

        [Column("agreement_date")]
        public DateTimeOffset AgreementDate { get; set; }


        [Association(ThisKey = nameof(DocumentId), OtherKey = nameof(DocumentDto.Id))]
        public DocumentDto Document { get; set; }

        [Association(ThisKey = nameof(AccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto Account { get; set; }
    }
}
