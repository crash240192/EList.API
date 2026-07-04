using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("account_album_rls")]
    public class AccountAlbumRelationDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("album_id")]
        public Guid AlbumId { get; set; }

        [Column("account_id")]
        public Guid AccountId { get; set; }

        [Association(ThisKey = nameof(AccountId), OtherKey = nameof(AccountDto.Id))]
        public AccountDto Acount { get; set; }

        [Association(ThisKey = nameof(AlbumId), OtherKey = nameof(MediaAlbumDto.Id))]
        public MediaAlbumDto Album { get; set; }
    }
}
