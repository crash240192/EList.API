using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("event_album_parameters")]
    public class EventAlbumParametersDto
    {
        [Column("album_id"), PrimaryKey, Identity]
        public Guid AlbumId { get;set; }

        [Column("head_album")]
        public bool HeadAlbum { get; set; }

        [Column("participants_readonly")]
        public bool ParticipantsReadonly { get; set; }

        [Column("privte_album")]
        public bool PrivateAlbum { get; set; }

        [Association(ThisKey = nameof(AlbumId), OtherKey = nameof(MediaAlbumDto.Id))]
        public MediaAlbumDto Album { get; set; }
    }

    //public class AlbumAllowedUsers
    //{
    //    [Column("id"), PrimaryKey, Identity]
    //    public Guid Id { get; set; }

    //    public Guid AccountId { get; set; }


    //}
}
