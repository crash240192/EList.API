using LinqToDB.Mapping;
using System.Net;

namespace EList.DbDataProvider.Models
{
    [Table("public.contact_data")]
    public class ContactDataDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { set; get; }

        [Column("type_id")]
        public Guid TypeId { get; set; }

        [Column("is_authorization_contact")]
        public bool IsAuthorizationContact { get; set; }

        [Column("show")]
        public bool Show { get; set; }

        [Column("value")]
        public string Value { get; set; }


        [Association(ThisKey = nameof(TypeId), OtherKey = nameof(ContactTypeDto.Id))]
        public ContactTypeDto ContactType { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(ContactAccountRelationDto.ContactId))]
        public ContactAccountRelationDto AccountRelation { get; set; }
        
    }
}
