using LinqToDB.Mapping;

namespace EList.DbDataProvider.Models.Enums
{
    /// <summary>
    /// Статус заказа
    /// </summary>
    public enum OrderStatus
    {
        [MapValue(Value = "pending")]
        Pending = 0,

        [MapValue(Value = "authorized")]
        Authorized = 1,

        [MapValue(Value = "paid")]
        Paid = 2,

        [MapValue(Value = "canceled")]
        Canceled = 3,

        [MapValue(Value = "refunded")]
        Refunded = 4,

        [MapValue(Value = "partially_refunded")]
        PartiallyRefunded = 5,

        [MapValue(Value = "failed")]
        Failed = 6
    }
}
