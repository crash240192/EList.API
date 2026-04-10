using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("public.contact_types")]
    public class ContactTypeDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("name_path")]
        public string NamePath { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("mask")]
        public string Mask { get; set; }

        [Column("allow_notifications")]
        public bool AllowNotifications { get; set; }
    }
}
