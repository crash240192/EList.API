using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("file_album_rls")]
    public class FileAlbumRelationDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("file_id")]
        public Guid FileId { get; set; }

        [Column("album_id")]
        public Guid AlbumId { get; set; }

        [Column("hidden")]
        public bool Hidden { get; set; }

        [Column("hidden_at")]
        public DateTimeOffset? HiddenAt { get; set; }

        [Column("hidden_by")]
        public Guid? HiddenBy { get; set; }

        [Association(ThisKey = nameof(AlbumId), OtherKey = nameof(MediaAlbumDto.Id))]
        public MediaAlbumDto AlbumDto { get; set; }
    }
}
