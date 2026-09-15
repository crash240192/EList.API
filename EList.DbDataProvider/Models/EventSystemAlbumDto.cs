using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("event_system_albums")]
    public class EventSystemAlbumDto
    {
        [Column("event_id"), PrimaryKey]
        public Guid EventId { get; set; }

        [Column("system_kind"), PrimaryKey]
        public short SystemKind { get; set; }

        [Column("album_id")]
        public Guid AlbumId { get; set; }
    }
}
