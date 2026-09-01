using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("account_album_parameters")]
    public class AccountAlbumParametersDto
    {
        [Column("album_id"), PrimaryKey]
        public Guid AlbumId { get; set; }

        [Column("head_album")]
        public bool HeadAlbum { get; set; }

        [Column("participants_readonly")]
        public bool ParticipantsReadonly { get; set; }

        [Column("private_album")]
        public bool Private { get; set; }

        [Association(ThisKey = nameof(AlbumId), OtherKey = nameof(MediaAlbumDto.Id))]
        public MediaAlbumDto Album { get; set; }
    }
}
