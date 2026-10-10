namespace EList.Models.Orders
{
    /// <summary>
    /// Отмена неоплаченного заказа покупателем (Pending / Authorized).
    /// </summary>
    public class CancelOrderRequest
    {
        /// <summary>Идентификатор заказа. Можно передать в path вместо body.</summary>
        public Guid OrderId { get; set; }
    }
}
