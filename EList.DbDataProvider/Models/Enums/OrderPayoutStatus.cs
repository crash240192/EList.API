using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    public enum OrderPayoutStatus
    {
        [MapValue(Value = "pending")]
        Pending = 0,

        [MapValue(Value = "initiated")]
        Initiated = 1,

        [MapValue(Value = "succeeded")]
        Succeeded = 2,

        [MapValue(Value = "failed")]
        Failed = 3,

        [MapValue(Value = "skipped")]
        Skipped = 4
    }
}
