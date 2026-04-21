using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models
{
    [Table("organizations")]
    public class OrganizationDto
    {
        [Column("id"), PrimaryKey, Identity]
        public Guid Id { get; set; }

        [Column("active")]
        public bool Active { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("address")]
        public string Address { get; set; }
        
        [Column("latitude")]
        public double? Latitude { get; set; }

        [Column("longitude")]
        public double? Longitude { get; set; }

        [Column("wallet_id")]
        public Guid WalletId { get; set; }
    }
}
