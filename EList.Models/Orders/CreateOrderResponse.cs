namespace EList.Models.Orders
{
    /// <summary>
    /// Результат создания заказа: сразу оплачен (free) или нужна оплата у провайдера.
    /// </summary>
    public class CreateOrderResponse
    {
        public OrderResponse Order { get; set; }

        /// <summary>URL подтверждения оплаты у провайдера (stub / ЮKassa).</summary>
        public string? ConfirmationUrl { get; set; }

        /// <summary>Идентификатор платежа у провайдера.</summary>
        public string? ProviderPaymentId { get; set; }

        /// <summary>true — заказ уже Paid, билеты выданы (например Cost = 0).</summary>
        public bool PaidImmediately { get; set; }
    }
}
