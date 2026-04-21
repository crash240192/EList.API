using EList.DbDataProvider.Models.Enums;
using LinqToDB;
using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("system_notifications")]
    public class SystemNotificationDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("type", DataType = DataType.Enum)]
        public SystemNotificationType Type { get; set; }

        [Column("header")]
        public string Header{ get; set; }

        [Column("message")]
        public string Message { get; set; }

        [Column("short_message")]
        public string ShortMessage { get; set; }
    }
}
