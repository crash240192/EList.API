using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("media_album_views")]
    public class MediaAlbumViewDto
    {
        [Column("account_id"), PrimaryKey(1)]
        public Guid AccountId { get; set; }

        [Column("album_id"), PrimaryKey(2)]
        public Guid AlbumId { get; set; }

        [Column("last_viewed_at")]
        public DateTimeOffset LastViewedAt { get; set; }
    }
}
