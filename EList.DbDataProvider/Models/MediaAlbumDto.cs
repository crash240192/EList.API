using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("media_albums")]
    public class MediaAlbumDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("account_id")]
        public Guid AccountId { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("event_id")]
        public Guid? EventId { get; set; }

        [Column("wallpaper_id")]
        public Guid? WallpaperId { get; set; }

        [Column("create_date")]
        public DateTimeOffset CreateDate { get; set; }

        [Column("update_date")]
        public DateTimeOffset UpdateDate { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(FileEventRelationDto.AlbumId))]
        public List<FileEventRelationDto> Files { get; set; }

        [Association(ThisKey = nameof(Id), OtherKey = nameof(EventAlbumParametersDto.AlbumId))]
        public EventAlbumParametersDto Parameters { get; set; }
    }
}

