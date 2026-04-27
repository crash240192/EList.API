using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("event_album_relation")]
    public class EventAlbumRelationDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("album_id")]
        public Guid AlbumId { get; set; }

        [Column("event_id")]
        public Guid EventId { get; set; }

        [Association(ThisKey = nameof(EventId), OtherKey = nameof(EventDto.Id))]
        public EventDto Event { get; set; }

        [Association(ThisKey = nameof(AlbumId), OtherKey = nameof(MediaAlbumDto.Id))]
        public MediaAlbumDto Album { get; set; }
    }
}
