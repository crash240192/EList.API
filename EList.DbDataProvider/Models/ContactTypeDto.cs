using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("contact_types")]
    public class ContactTypeDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("localization_path")]
        public string LocalizationPath { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("mask")]
        public string Mask { get; set; }

        [Column("allow_notifications")]
        public bool AllowNotifications { get; set; }

        [Column("active")]
        public bool Active { get; set; } = true;
    }
}
