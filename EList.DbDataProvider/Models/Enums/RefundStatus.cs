using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    /// <summary>
    /// Статус возврата
    /// </summary>
    public enum RefundStatus
    {
        [MapValue(Value = "pending")]
        Pending = 0,

        [MapValue(Value = "succeeded")]
        Succeeded = 1,

        [MapValue(Value = "failed")]
        Failed = 2
    }
}
