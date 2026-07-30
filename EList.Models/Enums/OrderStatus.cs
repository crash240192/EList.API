namespace EList.Models.Enums
{
    /// <summary>
    /// Статус заказа
    /// </summary>
    public enum OrderStatus
    {
        Pending = 0,
        Authorized = 1,
        Paid = 2,
        Canceled = 3,
        Refunded = 4,
        PartiallyRefunded = 5,
        Failed = 6
    }
}
