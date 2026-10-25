using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    public enum OrderPayoutKind
    {
        [MapValue(Value = "seller")]
        Seller = 0,

        [MapValue(Value = "platform_commission")]
        PlatformCommission = 1
    }
}
